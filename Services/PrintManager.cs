using System.Text.Json;
using rendezvous_companion.Models;
using rendezvous_companion.Templates;

namespace rendezvous_companion.Services;

public class PrintManager
{
    private IPrinterService? _receiptPrinter;
    private IPrinterService? _kitchenPrinter;

    private readonly PrintQueueService _queue;
    private readonly AlertService _alert;

    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public PrinterDevice? ReceiptPrinterDevice { get; private set; }
    public PrinterDevice? KitchenPrinterDevice { get; private set; }

    public bool IsReceiptPrinterConnected => _receiptPrinter?.IsConnected ?? false;
    public bool IsKitchenPrinterConnected => _kitchenPrinter?.IsConnected ?? false;

    public event Action? PrinterStatusChanged;

    public PrintManager(PrintQueueService queue, AlertService alert)
    {
        _queue = queue;
        _alert = alert;
    }

    // ─── Device Scanning ──────────────────────────────────────────────────────

    public async Task<List<PrinterDevice>> ScanDevicesAsync()
    {
        var all = new List<PrinterDevice>();

        var usbService = new UsbPrinterService();
        all.AddRange(await usbService.GetAvailableDevicesAsync());

        try
        {
            var btService = new BluetoothPrinterService();
            all.AddRange(await btService.GetAvailableDevicesAsync());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PrintManager] BT scan skipped: {ex.Message}");
        }

        return all;
    }

    // ─── Connect ──────────────────────────────────────────────────────────────

    public async Task<bool> ConnectReceiptPrinterAsync(PrinterDevice device)
    {
        _receiptPrinter = CreateService(device);
        var connected = await _receiptPrinter.ConnectAsync(device.Id);
        if (connected)
        {
            ReceiptPrinterDevice = device;
            DevicePreferencesService.SaveReceiptPrinter(device);
            _ = ReportStatusAsync();
            PrinterStatusChanged?.Invoke();
        }
        return connected;
    }

    public async Task<bool> ConnectKitchenPrinterAsync(PrinterDevice device)
    {
        _kitchenPrinter = CreateService(device);
        var connected = await _kitchenPrinter.ConnectAsync(device.Id);
        if (connected)
        {
            KitchenPrinterDevice = device;
            DevicePreferencesService.SaveKitchenPrinter(device);
            _ = ReportStatusAsync();
            PrinterStatusChanged?.Invoke();
        }
        return connected;
    }

    // ─── Auto-Reconnect (called on app resume) ────────────────────────────────

    public async Task TryAutoReconnectAsync()
    {
        var receiptDevice = DevicePreferencesService.LoadReceiptPrinter();
        var kitchenDevice = DevicePreferencesService.LoadKitchenPrinter();

        if (receiptDevice != null && !IsReceiptPrinterConnected)
        {
            Console.WriteLine($"[PrintManager] Auto-reconnecting receipt: {receiptDevice.Name}");
            await ConnectReceiptPrinterAsync(receiptDevice);
        }

        if (kitchenDevice != null && !IsKitchenPrinterConnected)
        {
            Console.WriteLine($"[PrintManager] Auto-reconnecting kitchen: {kitchenDevice.Name}");
            await ConnectKitchenPrinterAsync(kitchenDevice);
        }
    }

    // ─── Print Methods ────────────────────────────────────────────────────────

    public async Task<bool> PrintReceiptAsync(Order order)
    {
        if (_receiptPrinter?.IsConnected != true)
        {
            EnqueueFailed(order, PrintJobType.Receipt);
            return false;
        }
        var data = CustomerReceipt.Build(order);
        var ok = await _receiptPrinter.PrintAsync(data);
        if (!ok)
        {
            EnqueueFailed(order, PrintJobType.Receipt);
            _alert.NotifyPrintFailed();
        }
        return ok;
    }

    public async Task<bool> PrintKitchenSlipAsync(Order order)
    {
        // Only print kitchen slip if a dedicated kitchen printer is connected.
        // Do NOT fall back to receipt printer — that causes duplicate prints on a single device.
        if (_kitchenPrinter?.IsConnected != true)
        {
            EnqueueFailed(order, PrintJobType.Kitchen);
            return false;
        }
        var data = KitchenSlip.Build(order);
        var ok = await _kitchenPrinter.PrintAsync(data);
        if (!ok)
        {
            EnqueueFailed(order, PrintJobType.Kitchen);
            _alert.NotifyPrintFailed();
        }
        return ok;
    }

    public async Task<bool> PrintQRAsync(string url, string label, string target)
    {
        var data = QrSlip.Build(url, label);
        bool success = false;

        if (_receiptPrinter?.IsConnected == true)
            success = await _receiptPrinter.PrintAsync(data) || success;

        if (_kitchenPrinter?.IsConnected == true)
            success = await _kitchenPrinter.PrintAsync(data) || success;

        if (!success)
        {
            _queue.Enqueue(new PrintQueueItem
            {
                JobType = PrintJobType.QR,
                Status = PrintJobStatus.Failed,
                QrUrl = url,
                QrLabel = label,
                QrTarget = target,
            });
            _alert.NotifyPrintFailed();
        }

        return success;
    }

    public async Task<bool> PrintZReportAsync(ZReport report)
    {
        if (_receiptPrinter?.IsConnected != true)
        {
            _queue.Enqueue(new PrintQueueItem
            {
                JobType = report.IsXReading ? PrintJobType.XReport : PrintJobType.ZReport,
                Status = PrintJobStatus.Failed,
                Payload = JsonSerializer.Serialize(report, _json),
            });
            return false;
        }

        try
        {
            byte[] data = report.IsXReading ? XReportReceipt.Build(report) : ZReportReceipt.Build(report);
            var ok = await _receiptPrinter.PrintAsync(data);
            if (!ok) _alert.NotifyPrintFailed();
            return ok;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PrintManager] ZReport error: {ex.Message}");
            return false;
        }
    }

    public async Task<(bool receiptOk, bool kitchenOk)> PrintOrderAsync(Order order)
    {
        var receiptOk = await PrintReceiptAsync(order);
        var kitchenOk = await PrintKitchenSlipAsync(order);
        return (receiptOk, kitchenOk);
    }

    // ─── Retry a failed queue item ────────────────────────────────────────────

    public async Task<bool> RetryQueueItemAsync(PrintQueueItem item)
    {
        try
        {
            bool ok = false;

            if (item.JobType == PrintJobType.QR)
            {
                ok = await PrintQRAsync(item.QrUrl ?? "", item.QrLabel ?? "", item.QrTarget ?? "receipt");
            }
            else if (item.JobType is PrintJobType.ZReport or PrintJobType.XReport)
            {
                var report = JsonSerializer.Deserialize<ZReport>(item.Payload, _json);
                if (report != null) ok = await PrintZReportAsync(report);
            }
            else
            {
                var order = JsonSerializer.Deserialize<Order>(item.Payload, _json);
                if (order != null)
                {
                    ok = item.JobType == PrintJobType.Receipt
                        ? await PrintReceiptAsync(order)
                        : await PrintKitchenSlipAsync(order);
                }
            }

            if (ok)
                _queue.MarkPrinted(item.JobId);
            else
                _queue.MarkFailed(item.JobId);

            return ok;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PrintManager] RetryQueueItem error: {ex.Message}");
            _queue.MarkFailed(item.JobId);
            return false;
        }
    }

    // ─── Disconnect ───────────────────────────────────────────────────────────

    public async Task DisconnectAllAsync()
    {
        if (_receiptPrinter != null) await _receiptPrinter.DisconnectAsync();
        if (_kitchenPrinter != null) await _kitchenPrinter.DisconnectAsync();
        _ = ReportStatusAsync();
        PrinterStatusChanged?.Invoke();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void EnqueueFailed(Order order, PrintJobType type)
    {
        _queue.Enqueue(new PrintQueueItem
        {
            JobType = type,
            Status = PrintJobStatus.Failed,
            OrderId = order.OrderId,
            OrderNumber = order.OrderNumber,
            CustomerName = order.CustomerName,
            Payload = JsonSerializer.Serialize(order, _json),
        });
    }

    private async Task ReportStatusAsync()
    {
        var socketService = App.Current?.Handler.MauiContext?.Services.GetService<SocketService>();
        if (socketService != null)
            await socketService.ReportPrinterStatusAsync(IsReceiptPrinterConnected, IsKitchenPrinterConnected);
    }

    private IPrinterService CreateService(PrinterDevice device) =>
        device.ConnectionType == PrinterConnectionType.Bluetooth
            ? new BluetoothPrinterService()
            : new UsbPrinterService();
}

using rendezvous_companion.Models;
using rendezvous_companion.Templates;

namespace rendezvous_companion.Services;

public class PrintManager
{
    private IPrinterService? _receiptPrinter;
    private IPrinterService? _kitchenPrinter;

    public PrinterDevice? ReceiptPrinterDevice { get; private set; }
    public PrinterDevice? KitchenPrinterDevice { get; private set; }

    public bool IsReceiptPrinterConnected => _receiptPrinter?.IsConnected ?? false;
    public bool IsKitchenPrinterConnected => _kitchenPrinter?.IsConnected ?? false;

    /// <summary>
    /// Get all available devices (USB + Bluetooth combined)
    /// </summary>
    public async Task<List<PrinterDevice>> ScanDevicesAsync()
    {
        var allDevices = new List<PrinterDevice>();

        // Scan USB
        var usbService = new UsbPrinterService();
        var usbDevices = await usbService.GetAvailableDevicesAsync();
        allDevices.AddRange(usbDevices);

        // Scan Bluetooth
        try
        {
            var btService = new BluetoothPrinterService();
            var btDevices = await btService.GetAvailableDevicesAsync();
            allDevices.AddRange(btDevices);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"BT scan skipped: {ex.Message}");
        }

        return allDevices;
    }

    /// <summary>
    /// Connect a printer for receipt printing
    /// </summary>
    public async Task<bool> ConnectReceiptPrinterAsync(PrinterDevice device)
    {
        _receiptPrinter = CreateService(device);
        var connected = await _receiptPrinter.ConnectAsync(device.Id);
        if (connected)
        {
            ReceiptPrinterDevice = device;
            _ = ReportStatus();
        }
        return connected;
    }

    /// <summary>
    /// Connect a printer for kitchen slips
    /// </summary>
    public async Task<bool> ConnectKitchenPrinterAsync(PrinterDevice device)
    {
        _kitchenPrinter = CreateService(device);
        var connected = await _kitchenPrinter.ConnectAsync(device.Id);
        if (connected)
        {
            KitchenPrinterDevice = device;
            _ = ReportStatus();
        }
        return connected;
    }

    private async Task ReportStatus()
    {
        var socketService = App.Current?.Handler.MauiContext?.Services.GetService<SocketService>();
        if (socketService != null)
        {
            await socketService.ReportPrinterStatusAsync(
                IsReceiptPrinterConnected,
                IsKitchenPrinterConnected
            );
        }
    }

    /// <summary>
    /// Print both receipt and kitchen slip for an order
    /// </summary>
    public async Task<(bool receiptOk, bool kitchenOk)> PrintOrderAsync(Order order)
    {
        bool receiptOk = false;
        bool kitchenOk = false;

        // Print customer receipt
        if (_receiptPrinter?.IsConnected == true)
        {
            var receiptData = CustomerReceipt.Build(order);
            receiptOk = await _receiptPrinter.PrintAsync(receiptData);
        }

        // Print kitchen slip
        if (_kitchenPrinter?.IsConnected == true)
        {
            var kitchenData = KitchenSlip.Build(order);
            kitchenOk = await _kitchenPrinter.PrintAsync(kitchenData);
        }

        // If same printer for both, print sequentially
        if (_receiptPrinter?.IsConnected == true && _kitchenPrinter == null)
        {
            var kitchenData = KitchenSlip.Build(order);
            kitchenOk = await _receiptPrinter.PrintAsync(kitchenData);
        }

        return (receiptOk, kitchenOk);
    }

    /// <summary>
    /// Print a QR code (e.g. for Tables or Walk-ins).
    /// Always prints to all connected printers so one missing printer doesn't block.
    /// </summary>
    public async Task<bool> PrintQRAsync(string url, string label, string target)
    {
        var data = QrSlip.Build(url, label);
        bool success = false;

        Console.WriteLine(
            $"[PrintManager] PrintQRAsync called: url={url}, label={label}, target={target}"
        );
        Console.WriteLine(
            $"[PrintManager] Receipt connected: {IsReceiptPrinterConnected}, Kitchen connected: {IsKitchenPrinterConnected}"
        );

        // Try receipt printer
        if (_receiptPrinter?.IsConnected == true)
        {
            Console.WriteLine("[PrintManager] Sending to receipt printer...");
            var ok = await _receiptPrinter.PrintAsync(data);
            Console.WriteLine($"[PrintManager] Receipt printer result: {ok}");
            success = ok || success;
        }

        // Try kitchen printer
        if (_kitchenPrinter?.IsConnected == true)
        {
            Console.WriteLine("[PrintManager] Sending to kitchen printer...");
            var ok = await _kitchenPrinter.PrintAsync(data);
            Console.WriteLine($"[PrintManager] Kitchen printer result: {ok}");
            success = ok || success;
        }

        if (!success)
        {
            Console.WriteLine("[PrintManager] No printers available to print QR.");
        }

        return success;
    }

    /// <summary>
    /// Print only the kitchen slip
    /// </summary>
    public async Task<bool> PrintKitchenSlipAsync(Order order)
    {
        var printer = _kitchenPrinter ?? _receiptPrinter;
        if (printer?.IsConnected != true)
            return false;
        var data = KitchenSlip.Build(order);
        return await printer.PrintAsync(data);
    }

    /// <summary>
    /// Print only the customer receipt
    /// </summary>
    public async Task<bool> PrintReceiptAsync(Order order)
    {
        if (_receiptPrinter?.IsConnected != true)
            return false;
        var data = CustomerReceipt.Build(order);
        return await _receiptPrinter.PrintAsync(data);
    }

    public async Task DisconnectAllAsync()
    {
        if (_receiptPrinter != null)
            await _receiptPrinter.DisconnectAsync();
        if (_kitchenPrinter != null)
            await _kitchenPrinter.DisconnectAsync();
        _ = ReportStatus();
    }

    private IPrinterService CreateService(PrinterDevice device) =>
        device.ConnectionType == PrinterConnectionType.Bluetooth
            ? new BluetoothPrinterService()
            : new UsbPrinterService();
}

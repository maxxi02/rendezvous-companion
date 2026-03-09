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

    // Store settings
    public string StoreName { get; set; } = "RENDEZVOUS";
    public string StoreAddress { get; set; } = "";
    public string StoreTel { get; set; } = "";

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
            await socketService.ReportPrinterStatusAsync(IsReceiptPrinterConnected, IsKitchenPrinterConnected);
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
            var receiptData = CustomerReceipt.Build(order, StoreName, StoreAddress, StoreTel);
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
    /// Print a QR code (e.g. for Tables or Walk-ins)
    /// </summary>
    public async Task<bool> PrintQRAsync(string url, string label, string target)
    {
        bool success = false;
        var data = QrSlip.Build(url, label);

        if (target == "receipt" || target == "both")
        {
            if (_receiptPrinter?.IsConnected == true)
            {
                success = await _receiptPrinter.PrintAsync(data);
            }
        }
        
        if (target == "kitchen" || target == "both")
        {
            if (_kitchenPrinter?.IsConnected == true)
            {
                success = await _kitchenPrinter.PrintAsync(data) || success;
            }
        }

        // Fallback: If target was requested but printer is offline, try the other one.
        if (!success)
        {
             var fallbackPrinter = _receiptPrinter?.IsConnected == true ? _receiptPrinter : _kitchenPrinter;
             if (fallbackPrinter != null)
             {
                 success = await fallbackPrinter.PrintAsync(data);
             }
        }

        return success;
    }

    /// <summary>
    /// Print only the kitchen slip
    /// </summary>
    public async Task<bool> PrintKitchenSlipAsync(Order order)
    {
        var printer = _kitchenPrinter ?? _receiptPrinter;
        if (printer?.IsConnected != true) return false;
        var data = KitchenSlip.Build(order);
        return await printer.PrintAsync(data);
    }

    /// <summary>
    /// Print only the customer receipt
    /// </summary>
    public async Task<bool> PrintReceiptAsync(Order order)
    {
        if (_receiptPrinter?.IsConnected != true) return false;
        var data = CustomerReceipt.Build(order, StoreName, StoreAddress, StoreTel);
        return await _receiptPrinter.PrintAsync(data);
    }

    public async Task DisconnectAllAsync()
    {
        if (_receiptPrinter != null) await _receiptPrinter.DisconnectAsync();
        if (_kitchenPrinter != null) await _kitchenPrinter.DisconnectAsync();
        _ = ReportStatus();
    }

    private IPrinterService CreateService(PrinterDevice device)
        => device.ConnectionType == PrinterConnectionType.Bluetooth
            ? new BluetoothPrinterService()
            : new UsbPrinterService();
}
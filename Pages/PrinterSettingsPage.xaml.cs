using rendezvous_companion.Models;
using rendezvous_companion.Services;

namespace rendezvous_companion.Pages;

public partial class PrinterSettingsPage : ContentPage
{
    private readonly PrintManager _printManager;
    private List<PrinterDevice> _btDevices = new();
    private List<PrinterDevice> _usbDevices = new();

    public PrinterSettingsPage(PrintManager printManager)
    {
        InitializeComponent();
        _printManager = printManager;

        StoreNameEntry.Text = Preferences.Get("StoreName", "RENDEZVOUS");
        StoreAddressEntry.Text = Preferences.Get("StoreAddress", "");
        StoreTelEntry.Text = Preferences.Get("StoreTel", "");

        UpdateRoleSummary();
    }

    // ─── Bluetooth Scan ───────────────────────────────────────────

    private async void OnScanBluetoothClicked(object? sender, EventArgs e)
    {
        BtScanIndicator.IsVisible = true;
        BtScanIndicator.IsRunning = true;
        BtScanStatusLabel.Text = "Scanning for Bluetooth devices...";
        BluetoothDevicesList.ItemsSource = null;

        try
        {
            var btService = new BluetoothPrinterService();
            _btDevices = await btService.GetAvailableDevicesAsync();
            BluetoothDevicesList.ItemsSource = _btDevices;

            BtScanStatusLabel.Text = _btDevices.Count > 0
                ? $"Found {_btDevices.Count} Bluetooth device(s). Tap Receipt or Kitchen to assign."
                : "No Bluetooth devices found. Make sure your printer is paired and powered on.";
        }
        catch (Exception ex)
        {
            BtScanStatusLabel.Text = $"Bluetooth scan error: {ex.Message}";
        }
        finally
        {
            BtScanIndicator.IsVisible = false;
            BtScanIndicator.IsRunning = false;
        }
    }

    // ─── USB Scan ─────────────────────────────────────────────────

    private async void OnScanUsbClicked(object? sender, EventArgs e)
    {
        UsbScanIndicator.IsVisible = true;
        UsbScanIndicator.IsRunning = true;
        UsbScanStatusLabel.Text = "Scanning for USB devices...";
        UsbDevicesList.ItemsSource = null;

        try
        {
            var usbService = new UsbPrinterService();
            _usbDevices = await usbService.GetAvailableDevicesAsync();
            UsbDevicesList.ItemsSource = _usbDevices;

            UsbScanStatusLabel.Text = _usbDevices.Count > 0
                ? $"Found {_usbDevices.Count} USB device(s). Tap Receipt or Kitchen to assign."
                : "No USB devices found. Make sure your printer is plugged in.";
        }
        catch (Exception ex)
        {
            UsbScanStatusLabel.Text = $"USB scan error: {ex.Message}";
        }
        finally
        {
            UsbScanIndicator.IsVisible = false;
            UsbScanIndicator.IsRunning = false;
        }
    }

    // ─── Role Assignment ──────────────────────────────────────────

    private async void OnAssignReceiptClicked(object? sender, EventArgs e)
    {
        var device = GetDeviceFromButton(sender);
        if (device == null) return;

        var connected = await _printManager.ConnectReceiptPrinterAsync(device);

        if (connected)
        {
            await DisplayAlertAsync("Receipt Printer Set",
                $"{device.Name} [{device.ConnectionType}] assigned as Receipt Printer.", "OK");
        }
        else
        {
            await DisplayAlertAsync("Connection Failed",
                $"Could not connect to {device.Name}. Make sure it is on and in range.", "OK");
        }

        UpdateRoleSummary();
    }

    private async void OnAssignKitchenClicked(object? sender, EventArgs e)
    {
        var device = GetDeviceFromButton(sender);
        if (device == null) return;

        var connected = await _printManager.ConnectKitchenPrinterAsync(device);

        if (connected)
        {
            await DisplayAlertAsync("Kitchen Printer Set",
                $"{device.Name} [{device.ConnectionType}] assigned as Kitchen Printer.", "OK");
        }
        else
        {
            await DisplayAlertAsync("Connection Failed",
                $"Could not connect to {device.Name}. Make sure it is on and in range.", "OK");
        }

        UpdateRoleSummary();
    }

    // ─── Test Print ───────────────────────────────────────────────

    private async void OnTestReceiptClicked(object? sender, EventArgs e)
    {
        var ok = await _printManager.PrintReceiptAsync(CreateTestOrder());
        await DisplayAlertAsync(
            ok ? "Success" : "Failed",
            ok ? "Test receipt printed!" : "Print failed. Check receipt printer connection.",
            "OK");
    }

    private async void OnTestKitchenClicked(object? sender, EventArgs e)
    {
        var ok = await _printManager.PrintKitchenSlipAsync(CreateTestOrder());
        await DisplayAlertAsync(
            ok ? "Success" : "Failed",
            ok ? "Test kitchen slip printed!" : "Print failed. Check kitchen printer connection.",
            "OK");
    }

    // ─── Store Info ───────────────────────────────────────────────

    private void OnSaveStoreInfoClicked(object? sender, EventArgs e)
    {
        _printManager.StoreName = StoreNameEntry.Text ?? "RENDEZVOUS";
        _printManager.StoreAddress = StoreAddressEntry.Text ?? "";
        _printManager.StoreTel = StoreTelEntry.Text ?? "";

        Preferences.Set("StoreName", _printManager.StoreName);
        Preferences.Set("StoreAddress", _printManager.StoreAddress);
        Preferences.Set("StoreTel", _printManager.StoreTel);
    }

    // ─── Helpers ──────────────────────────────────────────────────

    private PrinterDevice? GetDeviceFromButton(object? sender)
    {
        if (sender is Button btn && btn.CommandParameter is PrinterDevice device)
            return device;
        return null;
    }

    private void UpdateRoleSummary()
    {
        // Receipt printer
        if (_printManager.ReceiptPrinterDevice != null)
        {
            ReceiptRoleLabel.Text = $"{_printManager.ReceiptPrinterDevice.Name} [{_printManager.ReceiptPrinterDevice.ConnectionType}]";
            ReceiptRoleLabel.TextColor = Colors.Green;
            TestReceiptButton.IsEnabled = true;
        }
        else
        {
            ReceiptRoleLabel.Text = "Not assigned";
            ReceiptRoleLabel.TextColor = Colors.Red;
            TestReceiptButton.IsEnabled = false;
        }

        // Kitchen printer
        if (_printManager.KitchenPrinterDevice != null)
        {
            KitchenRoleLabel.Text = $"{_printManager.KitchenPrinterDevice.Name} [{_printManager.KitchenPrinterDevice.ConnectionType}]";
            KitchenRoleLabel.TextColor = Colors.Green;
            TestKitchenButton.IsEnabled = true;
        }
        else
        {
            KitchenRoleLabel.Text = "Not assigned";
            KitchenRoleLabel.TextColor = Colors.Red;
            TestKitchenButton.IsEnabled = false;
        }
    }

    private Order CreateTestOrder() => new Order
    {
        OrderNumber = "TEST-001",
        OrderDate = DateTime.Now,
        TableNumber = 1,
        CustomerName = "Test Customer",
        CashReceived = 500,
        Items = new List<OrderItem>
        {
            new() { Name = "Burger", Quantity = 1, UnitPrice = 150, Notes = "No onions" },
            new() { Name = "Fries", Quantity = 2, UnitPrice = 60 },
            new() { Name = "Iced Tea", Quantity = 1, UnitPrice = 55, Notes = "Less sugar" }
        }
    };
}
using rendezvous_companion.Models;
using rendezvous_companion.Services;

namespace rendezvous_companion.Pages;

public partial class PrinterSettingsPage : ContentPage
{
    private readonly PrintManager _printManager;
    private List<PrinterDevice> _btDevices = new();
    private List<PrinterDevice> _usbDevices = new();

    // ─── Loading States ───────────────────────────────────────────

    private bool _isScanningBt;
    public bool IsScanningBt
    {
        get => _isScanningBt;
        set { _isScanningBt = value; OnPropertyChanged(); }
    }

    private bool _isScanningUsb;
    public bool IsScanningUsb
    {
        get => _isScanningUsb;
        set { _isScanningUsb = value; OnPropertyChanged(); }
    }

    private bool _isTestingReceipt;
    public bool IsTestingReceipt
    {
        get => _isTestingReceipt;
        set { _isTestingReceipt = value; OnPropertyChanged(); }
    }

    private bool _isTestingKitchen;
    public bool IsTestingKitchen
    {
        get => _isTestingKitchen;
        set { _isTestingKitchen = value; OnPropertyChanged(); }
    }

    public PrinterSettingsPage(PrintManager printManager)
    {
        InitializeComponent();
        _printManager = printManager;
        BindingContext = this;
        UpdateRoleSummary();
        UpdateThemeButtons(DevicePreferencesService.LoadTheme());
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Auto-scan both Bluetooth and USB when the page opens
        await Task.WhenAll(ScanBluetoothAsync(), ScanUsbAsync());
    }

    // ─── Bluetooth Scan ───────────────────────────────────────────

    private async void OnScanBluetoothClicked(object? sender, EventArgs e)
        => await ScanBluetoothAsync();

    private async Task ScanBluetoothAsync()
    {
        IsScanningBt = true;
        BtScanStatusLabel.Text = "Scanning for Bluetooth devices...";
        BluetoothDevicesList.ItemsSource = null;

        try
        {
            var btService = new BluetoothPrinterService();
            _btDevices = await btService.GetAvailableDevicesAsync();
            BluetoothDevicesList.ItemsSource = _btDevices;

            BtScanStatusLabel.Text = _btDevices.Count > 0
                ? $"Found {_btDevices.Count} device(s). Tap \"Pair\" on a new device, then assign it as Receipt or Kitchen."
                : "No Bluetooth devices found. Make sure your printer is powered on and discoverable.";
        }
        catch (Exception ex)
        {
            BtScanStatusLabel.Text = $"Bluetooth scan error: {ex.Message}";
        }
        finally
        {
            IsScanningBt = false;
        }
    }

    // ─── USB Scan ─────────────────────────────────────────────────

    private async void OnScanUsbClicked(object? sender, EventArgs e)
        => await ScanUsbAsync();

    private async Task ScanUsbAsync()
    {
        IsScanningUsb = true;
        UsbScanStatusLabel.Text = "Scanning for USB devices...";
        UsbDevicesList.ItemsSource = null;

        try
        {
            var usbService = new UsbPrinterService();
            _usbDevices = await usbService.GetAvailableDevicesAsync();
            UsbDevicesList.ItemsSource = _usbDevices;

            UsbScanStatusLabel.Text = _usbDevices.Count > 0
                ? $"Found {_usbDevices.Count} USB device(s). Tap \"Pair\" to grant permission, then assign as Receipt or Kitchen."
                : "No USB devices found. Make sure your printer is plugged in via OTG cable.";
        }
        catch (Exception ex)
        {
            UsbScanStatusLabel.Text = $"USB scan error: {ex.Message}";
        }
        finally
        {
            IsScanningUsb = false;
        }
    }

    // ─── Bluetooth Pairing ────────────────────────────────────────

    private async void OnPairBluetoothClicked(object? sender, EventArgs e)
    {
        var device = GetDeviceFromButton(sender);
        if (device == null) return;

        BtScanStatusLabel.Text = $"Pairing with {device.Name}…";

        try
        {
            var btService = new BluetoothPrinterService();
            var paired = await btService.PairAsync(device.Id);

            if (paired)
            {
                // Remove all other BT bonds so this is the only one
                await btService.UnpairOthersAsync(device.Id);

                // Refresh the list to update IsPaired badges
                _btDevices = await btService.GetAvailableDevicesAsync();
                BluetoothDevicesList.ItemsSource = _btDevices;

                BtScanStatusLabel.Text = $"{device.Name} paired! Now tap Receipt or Kitchen to assign it.";
            }
            else
            {
                BtScanStatusLabel.Text = $"Pairing failed or was cancelled for {device.Name}.";
            }
        }
        catch (Exception ex)
        {
            BtScanStatusLabel.Text = $"Pairing error: {ex.Message}";
        }
        finally
        {
            IsScanningBt = false;
        }
    }

    // ─── USB Pairing ──────────────────────────────────────────────

    private async void OnPairUsbClicked(object? sender, EventArgs e)
    {
        var device = GetDeviceFromButton(sender);
        if (device == null) return;

        UsbScanStatusLabel.Text = $"Requesting USB permission for {device.Name}…";

        try
        {
            var usbService = new UsbPrinterService();
            var granted = await usbService.PairAsync(device.Id);

            if (granted)
            {
                // Release USB claim on any other devices
                await usbService.UnpairOthersAsync(device.Id);

                // Refresh the list to update IsPaired (permission) badges
                _usbDevices = await usbService.GetAvailableDevicesAsync();
                UsbDevicesList.ItemsSource = _usbDevices;

                UsbScanStatusLabel.Text = $"Permission granted for {device.Name}! Now tap Receipt or Kitchen.";
            }
            else
            {
                UsbScanStatusLabel.Text = $"Permission denied for {device.Name}.";
            }
        }
        catch (Exception ex)
        {
            UsbScanStatusLabel.Text = $"USB permission error: {ex.Message}";
        }
        finally
        {
            IsScanningUsb = false;
        }
    }

    // ─── Role Assignment ──────────────────────────────────────────

    private async void OnAssignReceiptClicked(object? sender, EventArgs e)
    {
        var device = GetDeviceFromButton(sender);
        if (device == null)
            return;

        var connected = await _printManager.ConnectReceiptPrinterAsync(device);

        if (connected)
        {
            await DisplayAlertAsync(
                "Receipt Printer Set",
                $"{device.Name} [{device.ConnectionType}] assigned as Receipt Printer.",
                "OK"
            );
        }
        else
        {
            await DisplayAlertAsync(
                "Connection Failed",
                $"Could not connect to {device.Name}. Make sure it is on and in range.",
                "OK"
            );
        }

        UpdateRoleSummary();
    }

    private async void OnAssignKitchenClicked(object? sender, EventArgs e)
    {
        var device = GetDeviceFromButton(sender);
        if (device == null)
            return;

        var connected = await _printManager.ConnectKitchenPrinterAsync(device);

        if (connected)
        {
            await DisplayAlertAsync(
                "Kitchen Printer Set",
                $"{device.Name} [{device.ConnectionType}] assigned as Kitchen Printer.",
                "OK"
            );
        }
        else
        {
            await DisplayAlertAsync(
                "Connection Failed",
                $"Could not connect to {device.Name}. Make sure it is on and in range.",
                "OK"
            );
        }

        UpdateRoleSummary();
    }

    // ─── Test Print ───────────────────────────────────────────────

    private async void OnTestReceiptClicked(object? sender, EventArgs e)
    {
        IsTestingReceipt = true;
        try
        {
            var ok = await _printManager.PrintReceiptAsync(CreateTestOrder());
            await DisplayAlertAsync(ok ? "Success" : "Failed",
                ok ? "Test receipt printed!" : "Print failed. Check receipt printer connection.", "OK");
        }
        finally { IsTestingReceipt = false; }
    }

    private async void OnTestKitchenClicked(object? sender, EventArgs e)
    {
        IsTestingKitchen = true;
        try
        {
            var ok = await _printManager.PrintKitchenSlipAsync(CreateTestOrder());
            await DisplayAlertAsync(ok ? "Success" : "Failed",
                ok ? "Test kitchen slip printed!" : "Print failed. Check kitchen printer connection.", "OK");
        }
        finally { IsTestingKitchen = false; }
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
            ReceiptRoleLabel.Text =
                $"{_printManager.ReceiptPrinterDevice.Name} [{_printManager.ReceiptPrinterDevice.ConnectionType}]";
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
            KitchenRoleLabel.Text =
                $"{_printManager.KitchenPrinterDevice.Name} [{_printManager.KitchenPrinterDevice.ConnectionType}]";
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

    private Order CreateTestOrder() =>
        new Order
        {
            OrderNumber = "TEST-001",
            OrderDate = DateTime.Now,
            TableNumber = "1",
            CustomerName = "Test Customer",
            AmountPaid = 500,
            Items = new List<OrderItem>
            {
                new()
                {
                    Name = "Burger",
                    Quantity = 1,
                    Price = 150,
                    Notes = "No onions",
                },
                new()
                {
                    Name = "Fries",
                    Quantity = 2,
                    Price = 60,
                },
                new()
                {
                    Name = "Iced Tea",
                    Quantity = 1,
                    Price = 55,
                    Notes = "Less sugar",
                },
            },
        };

    // ─── Danger Zone ────────────────────────────────────────────────

    private async void OnExitAppClicked(object? sender, EventArgs e)
    {
        bool answer = await DisplayAlert(
            "Shutdown App",
            "Are you sure you want to stop the background service and exit the app? Your connected printers will be temporarily disconnected.",
            "Yes, Shutdown",
            "Cancel"
        );

        if (answer)
        {
            var appService =
                Application.Current?.Handler.MauiContext?.Services.GetService<rendezvous_companion.Services.IAppService>();
            appService?.StopAppAndService();

            // Fully close the application wrapper
            Application.Current?.Quit();

            // As a fallback for Windows, force process exit if Quit doesn't terminate background threads
#if WINDOWS
            Environment.Exit(0);
#endif
        }
    }

    // ─── Theme ────────────────────────────────────────────────────────────────

    private void OnThemeSystemClicked(object? sender, EventArgs e) => ApplyTheme(AppTheme.Unspecified);
    private void OnThemeLightClicked(object? sender, EventArgs e) => ApplyTheme(AppTheme.Light);
    private void OnThemeDarkClicked(object? sender, EventArgs e) => ApplyTheme(AppTheme.Dark);

    private void ApplyTheme(AppTheme theme)
    {
        Application.Current!.UserAppTheme = theme;
        DevicePreferencesService.SaveTheme(theme);
        UpdateThemeButtons(theme);
    }

    private void UpdateThemeButtons(AppTheme active)
    {
        var primary = (Color)Application.Current!.Resources["Primary"];
        var gray = (Color)Application.Current!.Resources["Gray400"];

        ThemeSystemBtn.BackgroundColor = active == AppTheme.Unspecified ? primary : gray;
        ThemeLightBtn.BackgroundColor  = active == AppTheme.Light        ? primary : gray;
        ThemeDarkBtn.BackgroundColor   = active == AppTheme.Dark         ? primary : gray;

        ThemeSystemBtn.TextColor = Colors.White;
        ThemeLightBtn.TextColor  = Colors.White;
        ThemeDarkBtn.TextColor   = Colors.White;
    }
}

using rendezvous_companion.Models;
using rendezvous_companion.Services;

namespace rendezvous_companion.Pages;

public partial class DashboardPage : ContentPage
{
    private readonly SocketService _socket;
    private readonly PrintManager _printManager;
    private readonly PrintQueueService _queue;

    // ─── Bindable Properties ──────────────────────────────────────────────────

    public Color SocketStatusColor => _socket.IsConnected
        ? Color.FromArgb("#28a745") : Color.FromArgb("#DC3545");

    public string SocketStatusText => _socket.IsConnected
        ? "Connected to server" : "Disconnected — tap Reconnect";

    public bool IsDisconnected => !_socket.IsConnected;

    public Color ReceiptPrinterColor => _printManager.IsReceiptPrinterConnected
        ? Color.FromArgb("#28a745") : Color.FromArgb("#DC3545");

    public Color KitchenPrinterColor => _printManager.IsKitchenPrinterConnected
        ? Color.FromArgb("#28a745") : Color.FromArgb("#DC3545");

    public string ReceiptPrinterName => _printManager.ReceiptPrinterDevice?.Name ?? "Not connected";
    public string KitchenPrinterName => _printManager.KitchenPrinterDevice?.Name ?? "Not connected";

    public string LastReceiptPrintTime { get; private set; } = "—";
    public string LastKitchenPrintTime { get; private set; } = "—";

    public int PendingCount => _queue.PendingCount;
    public int FailedCount => _queue.FailedCount;
    public int ActiveOrderCount => _socket.Orders.Count;

    public bool IsDarkMode => Application.Current?.UserAppTheme == AppTheme.Dark;

    public DashboardPage(SocketService socket, PrintManager printManager, PrintQueueService queue)
    {
        InitializeComponent();
        _socket = socket;
        _printManager = printManager;
        _queue = queue;
        BindingContext = this;

        _socket.ConnectionStatusChanged += _ => RefreshAll();
        _printManager.PrinterStatusChanged += RefreshAll;
        _queue.QueueChanged += RefreshAll;
        _socket.Orders.CollectionChanged += (_, _) => RefreshAll();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshAll();
    }

    private void RefreshAll()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OnPropertyChanged(nameof(SocketStatusColor));
            OnPropertyChanged(nameof(SocketStatusText));
            OnPropertyChanged(nameof(IsDisconnected));
            OnPropertyChanged(nameof(ReceiptPrinterColor));
            OnPropertyChanged(nameof(KitchenPrinterColor));
            OnPropertyChanged(nameof(ReceiptPrinterName));
            OnPropertyChanged(nameof(KitchenPrinterName));
            OnPropertyChanged(nameof(PendingCount));
            OnPropertyChanged(nameof(FailedCount));
            OnPropertyChanged(nameof(ActiveOrderCount));
        });
    }

    private async void OnReconnectClicked(object? sender, EventArgs e)
    {
        try
        {
            await _socket.ConnectAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Connection Failed", ex.Message, "OK");
        }
    }

    private async void OnPrintXReportClicked(object? sender, EventArgs e)
    {
        if (!_printManager.IsReceiptPrinterConnected)
        {
            await DisplayAlertAsync("No Printer", "Receipt printer is not connected.", "OK");
            return;
        }

        bool confirm = await DisplayAlertAsync("Print X-Report",
            "Print a mid-session X-Reading report now?", "Print", "Cancel");
        if (!confirm) return;

        var report = new ZReport
        {
            BusinessName = "Rendezvous Cafe",
            Today = DateTime.Now.ToString("MMMM dd, yyyy"),
            TimeNow = DateTime.Now.ToString("hh:mm tt"),
            IsXReading = true,
            Transactions = _socket.Orders.Count,
        };

        var ok = await _printManager.PrintZReportAsync(report);
        await DisplayAlertAsync(ok ? "Printed" : "Failed",
            ok ? "X-Report sent to printer." : "Could not print. Check printer connection.", "OK");
    }

    private void OnThemeToggled(object? sender, ToggledEventArgs e)
    {
        Application.Current!.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
        OnPropertyChanged(nameof(IsDarkMode));
    }
}

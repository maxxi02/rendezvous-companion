using rendezvous_companion.Services;

namespace rendezvous_companion.Pages;

public partial class DashboardPage : ContentPage
{
    private readonly SocketService _socket;
    private readonly PrintManager _printManager;

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

    private bool _isReconnecting;
    public bool IsReconnecting
    {
        get => _isReconnecting;
        set { _isReconnecting = value; OnPropertyChanged(); }
    }

    public DashboardPage(SocketService socket, PrintManager printManager)
    {
        InitializeComponent();
        _socket = socket;
        _printManager = printManager;
        BindingContext = this;

        _socket.ConnectionStatusChanged += _ => RefreshAll();
        _printManager.PrinterStatusChanged += RefreshAll;
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
        });
    }

    private async void OnReconnectClicked(object? sender, EventArgs e)
    {
        IsReconnecting = true;
        try
        {
            await _socket.ConnectAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Connection Failed", ex.Message, "OK");
        }
        finally
        {
            IsReconnecting = false;
        }
    }
}

using System.Collections.ObjectModel;
using System.Windows.Input;
using rendezvous_companion.Models;
using rendezvous_companion.Services;

namespace rendezvous_companion.Pages;

public partial class OrdersPage : ContentPage
{
    private readonly SocketService _socketService;
    private readonly PrintManager _printManager;

    public ObservableCollection<Order> Orders => _socketService.Orders;

    public ICommand PrintReceiptCommand { get; }
    public ICommand PrintKitchenCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand MarkPreparingCommand { get; }
    public ICommand MarkServingCommand { get; }
    public ICommand MarkCompletedCommand { get; }

    public bool IsRefreshing { get; set; }
    public string StatusMessage { get; set; } = "Connecting...";
    public bool IsConnected { get; set; }
    public Color ConnectionStatusColor => IsConnected
        ? Color.FromArgb("#28a745") : Color.FromArgb("#DC3545");

    public OrdersPage(SocketService socketService, PrintManager printManager)
    {
        InitializeComponent();
        _socketService = socketService;
        _printManager = printManager;
        BindingContext = this;

        PrintReceiptCommand = new Command<Order>(async o => await PrintReceipt(o));
        PrintKitchenCommand = new Command<Order>(async o => await PrintKitchen(o));
        RefreshCommand = new Command(async () => await RefreshOrders());
        MarkPreparingCommand = new Command<Order>(async o => await UpdateStatus(o, "preparing"));
        MarkServingCommand = new Command<Order>(async o => await UpdateStatus(o, "serving"));
        MarkCompletedCommand = new Command<Order>(async o => await UpdateStatus(o, "completed"));

        _socketService.ConnectionStatusChanged += OnConnectionChanged;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await _socketService.ConnectAsync();
            SetConnected(true);
        }
        catch
        {
            SetConnected(false);
        }
    }

    private void OnConnectionChanged(string status)
    {
        SetConnected(status == "connected");
    }

    private void SetConnected(bool connected)
    {
        IsConnected = connected;
        StatusMessage = connected ? "Connected to Server" : "Disconnected";
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OnPropertyChanged(nameof(IsConnected));
            OnPropertyChanged(nameof(StatusMessage));
            OnPropertyChanged(nameof(ConnectionStatusColor));
        });
    }

    private async Task RefreshOrders()
    {
        IsRefreshing = true;
        OnPropertyChanged(nameof(IsRefreshing));
        await Task.Delay(800);
        IsRefreshing = false;
        OnPropertyChanged(nameof(IsRefreshing));
    }

    private async Task PrintReceipt(Order order)
    {
        var ok = await _printManager.PrintReceiptAsync(order);
        if (ok)
            _socketService.MarkPrinted(order.OrderId, receipt: true);
        else
            await DisplayAlertAsync("Print Failed", "Could not print receipt. Check printer connection.", "OK");
    }

    private async Task PrintKitchen(Order order)
    {
        var foodItems = order.Items.Where(i => i.MenuType == "food").ToList();
        if (!foodItems.Any())
        {
            await DisplayAlertAsync("No Food Items", "This order has no food items for the kitchen.", "OK");
            return;
        }

        var kitchenOrder = new Order
        {
            OrderId = order.OrderId,
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            TableNumber = order.TableNumber,
            CustomerName = order.CustomerName,
            Items = foodItems,
        };

        var ok = await _printManager.PrintKitchenSlipAsync(kitchenOrder);
        if (ok)
            _socketService.MarkPrinted(order.OrderId, kitchen: true);
        else
            await DisplayAlertAsync("Print Failed", "Could not print kitchen slip. Check printer connection.", "OK");
    }

    private async Task UpdateStatus(Order order, string newStatus)
    {
        await _socketService.UpdateOrderStatusAsync(order.OrderId, newStatus);
        // Optimistic local update
        order.QueueStatus = newStatus;
        var idx = Orders.IndexOf(order);
        if (idx >= 0) Orders[idx] = order;
    }
}

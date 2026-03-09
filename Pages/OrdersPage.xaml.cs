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

    public bool IsRefreshing { get; set; }
    public string StatusMessage { get; set; } = "Connecting...";
    public bool IsConnected { get; set; }
    public Color ConnectionStatusColor => IsConnected ? Colors.Green : Colors.Red;

    public OrdersPage(SocketService socketService, PrintManager printManager)
    {
        InitializeComponent();
        _socketService = socketService;
        _printManager = printManager;

        BindingContext = this;

        PrintReceiptCommand = new Command<Order>(async (order) => await PrintReceipt(order));
        PrintKitchenCommand = new Command<Order>(async (order) => await PrintKitchen(order));
        RefreshCommand = new Command(async () => await RefreshOrders());

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await _socketService.ConnectAsync();
        StatusMessage = "Connected to Server";
        IsConnected = true;
        OnPropertyChanged(nameof(StatusMessage));
        OnPropertyChanged(nameof(IsConnected));
        OnPropertyChanged(nameof(ConnectionStatusColor));
    }

    private async Task RefreshOrders()
    {
        IsRefreshing = true;
        OnPropertyChanged(nameof(IsRefreshing));

        // In a real app, you might fetch initial orders via REST
        await Task.Delay(1000);

        IsRefreshing = false;
        OnPropertyChanged(nameof(IsRefreshing));
    }

    private async Task PrintReceipt(Order order)
    {
        var ok = await _printManager.PrintReceiptAsync(order);
        if (!ok)
        {
            await DisplayAlertAsync("Print Failed", "Could not print receipt. Please check printer connection.", "OK");
        }
    }

    private async Task PrintKitchen(Order order)
    {
        // Filter for food items
        var foodItems = order.Items.Where(i => i.MenuType == "food").ToList();
        if (!foodItems.Any())
        {
            await DisplayAlertAsync("No Food Items", "This order does not contain any food items to print for kitchen.", "OK");
            return;
        }

        // Create a copy of the order with only food items
        var kitchenOrder = new Order
        {
            OrderId = order.OrderId,
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            TableNumber = order.TableNumber,
            CustomerName = order.CustomerName,
            Items = foodItems
        };

        var ok = await _printManager.PrintKitchenSlipAsync(kitchenOrder);
        if (!ok)
        {
            await DisplayAlertAsync("Print Failed", "Could not print kitchen slip. Please check kitchen printer connection.", "OK");
        }
    }
}

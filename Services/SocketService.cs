using System.Collections.ObjectModel;
using SocketIOClient;
using rendezvous_companion.Models;
using System.Text.Json;

namespace rendezvous_companion.Services;

public class SocketService
{
    private readonly SocketIOClient.SocketIO _client;
    private readonly string _serverUrl;

    public ObservableCollection<Order> Orders { get; } = new();

    public event Action<string>? ConnectionStatusChanged;
    public event Action<Order>? OrderStatusChanged;

    public bool IsConnected => _client.Connected;

    public SocketService()
    {
        // ⚠️ Replace with your server's LAN IP (find it with `ipconfig` on Windows)
        // Must be reachable from the Android device on the same network
        _serverUrl = "https://rendezvous-server-gpmv.onrender.com";

        _client = new SocketIOClient.SocketIO(_serverUrl, new SocketIOOptions
        {
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
            ReconnectionAttempts = int.MaxValue,
            ReconnectionDelay = 2000
        });

        RegisterEvents();
    }

    private void RegisterEvents()
    {
        _client.OnConnected += (sender, e) =>
        {
            Console.WriteLine($"[Socket] Connected to {_serverUrl}");
            
            // 1. Join the POS room to receive background updates
            _client.EmitAsync("pos:join");

            // 2. Report current printer status
            var printManager = App.Current?.Handler.MauiContext?.Services.GetService<PrintManager>();
            if (printManager != null)
            {
                _ = ReportPrinterStatusAsync(printManager.IsReceiptPrinterConnected, printManager.IsKitchenPrinterConnected);
            }

            // 3. Fetch the current list of active orders
            _client.EmitAsync("order:queue:list", new { statuses = new[] { "pending_payment", "queueing", "preparing", "serving" } });

            MainThread.BeginInvokeOnMainThread(() =>
                ConnectionStatusChanged?.Invoke("connected"));
        };

        _client.OnDisconnected += (sender, reason) =>
        {
            Console.WriteLine($"[Socket] Disconnected: {reason}");
            MainThread.BeginInvokeOnMainThread(() =>
                ConnectionStatusChanged?.Invoke("disconnected"));
        };

        _client.OnError += (sender, error) =>
        {
            Console.WriteLine($"[Socket] Error: {error}");
        };

        // ── Emitted by server when a new order is created or payment confirmed ──
        _client.On("order:queue:updated", response =>
        {
            try
            {
                var data = response.GetValue<JsonElement>();
                if (data.TryGetProperty("order", out var orderElement))
                {
                    var order = JsonSerializer.Deserialize<Order>(
                        orderElement.GetRawText(),
                        JsonOptions);

                    if (order != null)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            var existing = Orders.FirstOrDefault(o => o.OrderId == order.OrderId);
                            if (existing != null)
                            {
                                var index = Orders.IndexOf(existing);
                                Orders[index] = order;
                            }
                            else
                            {
                                Orders.Insert(0, order);
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Socket] order:queue:updated error: {ex.Message}");
            }
        });

        // ── Emitted by server when an order's status changes ──
        _client.On("order:status:changed", response =>
        {
            try
            {
                var data = response.GetValue<JsonElement>();

                var orderId = data.TryGetProperty("orderId", out var id)
                    ? id.GetString() : null;
                var queueStatus = data.TryGetProperty("queueStatus", out var qs)
                    ? qs.GetString() : null;

                if (orderId == null) return;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var existing = Orders.FirstOrDefault(o => o.OrderId == orderId);
                    if (existing != null && queueStatus != null)
                    {
                        existing.QueueStatus = queueStatus;
                        var index = Orders.IndexOf(existing);
                        Orders[index] = existing; // trigger UI refresh
                    }

                    // Notify any listeners (e.g. a status page)
                    if (existing != null)
                        OrderStatusChanged?.Invoke(existing);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Socket] order:status:changed error: {ex.Message}");
            }
        });

        // ── Emitted by server after order:queue:list request ──
        _client.On("order:queue:list:result", response =>
        {
            try
            {
                var orders = response.GetValue<List<Order>>();
                if (orders != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Orders.Clear();
                        foreach (var order in orders.OrderByDescending(o => o.OrderDate))
                        {
                            Orders.Add(order);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Socket] order:queue:list:result error: {ex.Message}");
            }
        });

        // ── Receive print job from POS ──
        _client.On("print:job", async response =>
        {
            try
            {
                var job = response.GetValue<JsonElement>();
                Console.WriteLine($"[Socket] Print Job Received: {job.GetProperty("jobId")}");
                
                var target = job.GetProperty("target").GetString();
                var input = job.GetProperty("input");
                
                var order = JsonSerializer.Deserialize<Order>(input.GetRawText(), JsonOptions);
                if (order != null)
                {
                    // Immediately surface the order in the companion's Orders tab
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var existing = Orders.FirstOrDefault(o => o.OrderId == order.OrderId);
                        if (existing != null)
                        {
                            var index = Orders.IndexOf(existing);
                            Orders[index] = order;
                        }
                        else
                        {
                            Orders.Insert(0, order);
                        }
                    });

                    var printManager = App.Current?.Handler.MauiContext?.Services.GetService<PrintManager>();
                    if (printManager != null)
                    {
                        bool receiptPrinted = false;
                        bool kitchenPrinted = false;

                        if (target == "receipt" || target == "both")
                            receiptPrinted = await printManager.PrintReceiptAsync(order);
                        
                        if (target == "kitchen" || target == "both")
                            kitchenPrinted = await printManager.PrintKitchenSlipAsync(order);

                        await _client.EmitAsync("print:job:result", new { 
                            jobId = job.GetProperty("jobId").GetString(), 
                            success = true,
                            receipt = receiptPrinted || target == "receipt" || target == "both",
                            kitchen = kitchenPrinted || target == "kitchen" || target == "both"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Socket] print:job error: {ex.Message}");
            }
        });

        // ── Receive Z-Report print job from POS ──
        _client.On("print:zreport", async response =>
        {
            Console.WriteLine("[Socket] Z-Report Print Job Received (not implemented yet)");
        });

        // ── Emitted by server when a table is updated ──
        _client.On("table:updated", response =>
        {
            try
            {
                var data = response.GetValue<JsonElement>();
                Console.WriteLine($"[Socket] table:updated: {data}");
                // Hook into your table model here if needed
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Socket] table:updated error: {ex.Message}");
            }
        });
    }

    public async Task ConnectAsync()
    {
        if (_client.Connected) return;

        try
        {
            await _client.ConnectAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Socket] ConnectAsync failed: {ex.Message}");
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        if (!_client.Connected) return;

        try
        {
            await _client.DisconnectAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Socket] DisconnectAsync failed: {ex.Message}");
        }
    }

    public async Task ReportPrinterStatusAsync(bool usb, bool bluetooth)
    {
        if (!_client.Connected) return;
        await _client.EmitAsync("companion:printer:status", new { usb, bt = bluetooth });
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
using System.Collections.ObjectModel;
using System.Text.Json;
using rendezvous_companion.Models;
using SocketIOClient;

namespace rendezvous_companion.Services;

public class SocketService
{
    private readonly SocketIOClient.SocketIO _client;
    private readonly string _serverUrl;
    private readonly AlertService _alert;
    private readonly PrintQueueService _queue;

    public ObservableCollection<Order> Orders { get; } = new();

    // Tracks which orders have been printed: orderId -> (receipt, kitchen)
    public Dictionary<string, (bool Receipt, bool Kitchen)> PrintHistory { get; } = new();

    public event Action<string>? ConnectionStatusChanged;
    public event Action<Order>? OrderStatusChanged;
    public event Action<Order>? NewOrderArrived;

    public bool IsConnected => _client.Connected;

    public SocketService(AlertService alert, PrintQueueService queue)
    {
        _alert = alert;
        _queue = queue;
        _serverUrl = "https://rendezvous-server-gpmv.onrender.com";

        _client = new SocketIOClient.SocketIO(
            _serverUrl,
            new SocketIOOptions
            {
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
                ReconnectionAttempts = int.MaxValue,
                ReconnectionDelay = 2000,
            }
        );

        RegisterEvents();
    }

    // ─── Event Registration ───────────────────────────────────────────────────

    private void RegisterEvents()
    {
        _client.OnConnected += (sender, e) =>
        {
            Console.WriteLine($"[Socket] Connected to {_serverUrl}");
            _client.EmitAsync("pos:join");

            var printManager = App.Current?.Handler.MauiContext?.Services.GetService<PrintManager>();
            if (printManager != null)
                _ = ReportPrinterStatusAsync(printManager.IsReceiptPrinterConnected, printManager.IsKitchenPrinterConnected);

            _client.EmitAsync("order:queue:list",
                new { statuses = new[] { "pending_payment", "queueing", "preparing", "serving" } });

            MainThread.BeginInvokeOnMainThread(() => ConnectionStatusChanged?.Invoke("connected"));
        };

        _client.OnDisconnected += (sender, reason) =>
        {
            Console.WriteLine($"[Socket] Disconnected: {reason}");
            MainThread.BeginInvokeOnMainThread(() => ConnectionStatusChanged?.Invoke("disconnected"));
        };

        _client.OnError += (sender, error) => Console.WriteLine($"[Socket] Error: {error}");

        // ── New / updated order ──
        _client.On("order:queue:updated", response =>
        {
            try
            {
                var data = response.GetValue<JsonElement>();
                if (!data.TryGetProperty("order", out var orderElement)) return;

                var order = JsonSerializer.Deserialize<Order>(orderElement.GetRawText(), JsonOptions);
                if (order == null) return;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var existing = Orders.FirstOrDefault(o => o.OrderId == order.OrderId);
                    if (existing != null)
                    {
                        Orders[Orders.IndexOf(existing)] = order;
                    }
                    else
                    {
                        Orders.Insert(0, order);
                        // Alert only for brand-new orders
                        await _alert.NotifyNewOrderAsync();
                        NewOrderArrived?.Invoke(order);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Socket] order:queue:updated error: {ex.Message}");
            }
        });

        // ── Status change ──
        _client.On("order:status:changed", response =>
        {
            try
            {
                var data = response.GetValue<JsonElement>();
                var orderId = data.TryGetProperty("orderId", out var id) ? id.GetString() : null;
                var queueStatus = data.TryGetProperty("queueStatus", out var qs) ? qs.GetString() : null;
                if (orderId == null) return;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var existing = Orders.FirstOrDefault(o => o.OrderId == orderId);
                    if (existing != null && queueStatus != null)
                    {
                        existing.QueueStatus = queueStatus;
                        Orders[Orders.IndexOf(existing)] = existing;
                        OrderStatusChanged?.Invoke(existing);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Socket] order:status:changed error: {ex.Message}");
            }
        });

        // ── Queue list result ──
        _client.On("order:queue:list:result", response =>
        {
            try
            {
                var orders = response.GetValue<List<Order>>();
                if (orders == null) return;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Orders.Clear();
                    foreach (var o in orders.OrderByDescending(x => x.OrderDate))
                        Orders.Add(o);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Socket] order:queue:list:result error: {ex.Message}");
            }
        });

        // ── Print job ──
        _client.On("print:job", async response =>
        {
            try
            {
                var job = response.GetValue<JsonElement>();
                var jobId = job.GetProperty("jobId").GetString();
                var target = job.GetProperty("target").GetString();
                var input = job.GetProperty("input");

                var order = JsonSerializer.Deserialize<Order>(input.GetRawText(), JsonOptions);
                if (order == null) return;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var existing = Orders.FirstOrDefault(o => o.OrderId == order.OrderId);
                    if (existing != null)
                        Orders[Orders.IndexOf(existing)] = order;
                    else
                        Orders.Insert(0, order);
                });

                var printManager = App.Current?.Handler.MauiContext?.Services.GetService<PrintManager>();
                if (printManager == null) return;

                bool receiptPrinted = false;
                bool kitchenPrinted = false;

                if (target == "receipt" || target == "both")
                {
                    receiptPrinted = await printManager.PrintReceiptAsync(order);
                    if (receiptPrinted) MarkPrinted(order.OrderId, receipt: true);
                }

                if (target == "kitchen" || target == "both")
                {
                    kitchenPrinted = await printManager.PrintKitchenSlipAsync(order);
                    if (kitchenPrinted) MarkPrinted(order.OrderId, kitchen: true);
                }

                await _client.EmitAsync("print:job:result", new
                {
                    jobId,
                    success = receiptPrinted || kitchenPrinted,
                    receipt = receiptPrinted,
                    kitchen = kitchenPrinted,
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Socket] print:job error: {ex.Message}");
            }
        });

        // ── QR print ──
        _client.On("print:qr", async response =>
        {
            try
            {
                var data = response.GetValue<JsonElement>();
                var jobId = data.GetProperty("jobId").GetString();
                var url = data.GetProperty("url").GetString();
                var label = data.GetProperty("label").GetString();
                var target = data.TryGetProperty("target", out var t) ? t.GetString() : "receipt";

                var printManager = App.Current?.Handler.MauiContext?.Services.GetService<PrintManager>();
                if (printManager == null || url == null || label == null) return;

                bool success = await printManager.PrintQRAsync(url, label, target ?? "receipt");

                await _client.EmitAsync("print:job:result", new
                {
                    jobId,
                    success,
                    receipt = target == "receipt",
                    kitchen = target == "kitchen",
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Socket] print:qr error: {ex.Message}");
            }
        });

        // ── Z-Report ──
        _client.On("print:zreport", async response =>
        {
            try
            {
                var dict = response.GetValue<Dictionary<string, JsonElement>>();
                if (!dict.TryGetValue("data", out var dataElement)) return;

                var report = JsonSerializer.Deserialize<ZReport>(dataElement.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (report == null) return;

                var printManager = App.Current?.Handler.MauiContext?.Services.GetService<PrintManager>();
                if (printManager == null) return;

                bool success = await printManager.PrintZReportAsync(report);

                if (dict.TryGetValue("jobId", out var jobIdEl))
                {
                    await _client.EmitAsync("print:job:result", new
                    {
                        jobId = jobIdEl.GetString(),
                        success,
                        receipt = true,
                        kitchen = false,
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Socket] print:zreport error: {ex.Message}");
            }
        });

        _client.On("table:updated", response =>
        {
            try
            {
                var data = response.GetValue<JsonElement>();
                Console.WriteLine($"[Socket] table:updated: {data}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Socket] table:updated error: {ex.Message}");
            }
        });
    }

    // ─── Order Status Emit (from companion to server) ─────────────────────────

    public async Task UpdateOrderStatusAsync(string orderId, string newStatus)
    {
        if (!_client.Connected) return;
        await _client.EmitAsync("order:status:update", new { orderId, queueStatus = newStatus });
    }

    // ─── Print History ────────────────────────────────────────────────────────

    public void MarkPrinted(string orderId, bool receipt = false, bool kitchen = false)
    {
        if (!PrintHistory.TryGetValue(orderId, out var existing))
            existing = (false, false);
        PrintHistory[orderId] = (existing.Receipt || receipt, existing.Kitchen || kitchen);
    }

    public (bool Receipt, bool Kitchen) GetPrintStatus(string orderId) =>
        PrintHistory.TryGetValue(orderId, out var s) ? s : (false, false);

    // ─── Connection ───────────────────────────────────────────────────────────

    public async Task ConnectAsync()
    {
        if (_client.Connected) return;
        try { await _client.ConnectAsync(); }
        catch (Exception ex) { Console.WriteLine($"[Socket] ConnectAsync failed: {ex.Message}"); throw; }
    }

    public async Task DisconnectAsync()
    {
        if (!_client.Connected) return;
        try { await _client.DisconnectAsync(); }
        catch (Exception ex) { Console.WriteLine($"[Socket] DisconnectAsync failed: {ex.Message}"); }
    }

    public async Task ReportPrinterStatusAsync(bool usb, bool bluetooth)
    {
        if (!_client.Connected) return;
        await _client.EmitAsync("companion:printer:status", new { usb, bt = bluetooth });
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
}

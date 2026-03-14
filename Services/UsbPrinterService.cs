using System.IO.Ports;
#if ANDROID
using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Microsoft.Maui.ApplicationModel;
#endif

namespace rendezvous_companion.Services;

public class UsbPrinterService : IPrinterService
{
#if WINDOWS
    private SerialPort? _serialPort;
#endif
#if ANDROID
    private UsbDevice? _usbDevice;
    private UsbDeviceConnection? _usbConnection;
    private UsbEndpoint? _outEndpoint;

    // Static so that UnpairOthersAsync can close connections opened anywhere
    private static readonly Dictionary<string, UsbDeviceConnection> _openConnections = new();
#endif

    public bool IsConnected =>
#if WINDOWS
        _serialPort?.IsOpen ?? false;
#elif ANDROID
        _usbConnection != null;
#else
        false;
#endif

    // ─── GetAvailableDevicesAsync ──────────────────────────────────

    public async Task<List<PrinterDevice>> GetAvailableDevicesAsync()
    {
        var devices = new List<PrinterDevice>();

#if WINDOWS
        await Task.Run(() =>
        {
            try
            {
                var ports = SerialPort.GetPortNames();
                foreach (var port in ports)
                {
                    devices.Add(new PrinterDevice
                    {
                        Id             = port,
                        Name           = port,
                        ConnectionType = PrinterConnectionType.USB,
                        IsPaired       = true   // COM ports are always accessible on Windows
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"USB scan error: {ex.Message}");
            }
        });
#elif ANDROID
        await Task.Run(() =>
        {
            var usbManager = (UsbManager?)Android.App.Application.Context.GetSystemService(Context.UsbService);
            if (usbManager?.DeviceList?.Values == null) return;

            foreach (var device in usbManager.DeviceList.Values)
            {
                devices.Add(new PrinterDevice
                {
                    Id             = device.DeviceName,
                    Name           = $"{device.ProductName ?? "USB Device"} ({device.VendorId}:{device.ProductId})",
                    ConnectionType = PrinterConnectionType.USB,
                    IsPaired       = usbManager.HasPermission(device)
                });
            }
        });
#else
        await Task.CompletedTask;
#endif
        return devices;
    }

    // ─── PairAsync ────────────────────────────────────────────────
    // Requests USB permission for the specified device via an Android
    // BroadcastReceiver and waits for the system dialog result.

    public async Task<bool> PairAsync(string deviceId)
    {
#if ANDROID
        var usbManager = (UsbManager?)Android.App.Application.Context.GetSystemService(Context.UsbService);
        if (usbManager?.DeviceList?.Values == null) return false;

        var device = usbManager.DeviceList.Values.FirstOrDefault(d => d.DeviceName == deviceId);
        if (device == null) return false;

        // Already permitted
        if (usbManager.HasPermission(device)) return true;

        var tcs = new TaskCompletionSource<bool>();
        const string actionPermission = "com.solarworks.USB_PERMISSION";

        var receiver = new UsbPermissionReceiver(deviceId, tcs);
        Android.App.Application.Context.RegisterReceiver(
            receiver,
            new IntentFilter(actionPermission)
        );

        try
        {
#pragma warning disable CA1416
            var flags = Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M
                ? PendingIntentFlags.Immutable
                : 0;
            var intent = PendingIntent.GetBroadcast(
                Android.App.Application.Context, 0,
                new Intent(actionPermission),
                flags
            );
            usbManager.RequestPermission(device, intent);
#pragma warning restore CA1416

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            cts.Token.Register(() => tcs.TrySetResult(false));
            return await tcs.Task;
        }
        finally
        {
            try { Android.App.Application.Context.UnregisterReceiver(receiver); }
            catch { /* already unregistered */ }
        }
#else
        await Task.CompletedTask;
        return true;
#endif
    }

    // ─── UnpairOthersAsync ────────────────────────────────────────
    // Closes any open UsbDeviceConnection for every USB device except
    // the keeper. Android does not expose a runtime permission revocation
    // API, so releasing the connection is the best we can do.

    public async Task UnpairOthersAsync(string keepDeviceId)
    {
#if ANDROID
        await Task.Run(() =>
        {
            var toClose = _openConnections
                .Where(kv => kv.Key != keepDeviceId)
                .ToList();

            foreach (var kv in toClose)
            {
                try
                {
                    kv.Value.Close();
                    kv.Value.Dispose();
                    Console.WriteLine($"[USB] Released connection: {kv.Key}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[USB] Error releasing {kv.Key}: {ex.Message}");
                }
                _openConnections.Remove(kv.Key);
            }
        });
#else
        await Task.CompletedTask;
#endif
    }

    // ─── ConnectAsync ─────────────────────────────────────────────

    public async Task<bool> ConnectAsync(string deviceId)
    {
#if WINDOWS
        try
        {
            await Task.Run(() =>
            {
                _serialPort = new SerialPort(deviceId)
                {
                    BaudRate    = 9600,
                    DataBits    = 8,
                    Parity      = Parity.None,
                    StopBits    = StopBits.One,
                    ReadTimeout = 500,
                    WriteTimeout= 500
                };
                _serialPort.Open();
            });
            return IsConnected;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"USB Connection failed: {ex.Message}");
            return false;
        }
#elif ANDROID
        try
        {
            var usbManager = (UsbManager?)Android.App.Application.Context.GetSystemService(Context.UsbService);
            if (usbManager?.DeviceList?.Values == null) return false;

            _usbDevice = usbManager.DeviceList.Values.FirstOrDefault(d => d.DeviceName == deviceId);
            if (_usbDevice == null) return false;

            if (!usbManager.HasPermission(_usbDevice))
            {
                Console.WriteLine("[USB] No permission — call PairAsync first.");
                return false;
            }

            _usbConnection = usbManager.OpenDevice(_usbDevice);
            if (_usbConnection == null) return false;

            for (int i = 0; i < _usbDevice.InterfaceCount; i++)
            {
                var @interface = _usbDevice.GetInterface(i);
                for (int j = 0; j < @interface.EndpointCount; j++)
                {
                    var endpoint = @interface.GetEndpoint(j);
                    if (endpoint?.Type == UsbAddressing.XferBulk && endpoint.Direction == UsbAddressing.Out)
                    {
                        _usbConnection.ClaimInterface(@interface, true);
                        _outEndpoint = endpoint;

                        // Track the open connection for UnpairOthersAsync
                        _openConnections[deviceId] = _usbConnection;
                        return true;
                    }
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"USB Connection failed: {ex.Message}");
            return false;
        }
#else
        await Task.CompletedTask;
        return false;
#endif
    }

    // ─── DisconnectAsync ──────────────────────────────────────────

    public async Task DisconnectAsync()
    {
#if WINDOWS
        await Task.Run(() =>
        {
            _serialPort?.Close();
            _serialPort?.Dispose();
            _serialPort = null;
        });
#elif ANDROID
        await Task.Run(() =>
        {
            if (_usbDevice != null)
                _openConnections.Remove(_usbDevice.DeviceName);

            _usbConnection?.Close();
            _usbConnection?.Dispose();
            _usbConnection  = null;
            _outEndpoint    = null;
            _usbDevice      = null;
        });
#else
        await Task.CompletedTask;
#endif
    }

    // ─── PrintAsync ───────────────────────────────────────────────

    public async Task<bool> PrintAsync(byte[] data)
    {
#if WINDOWS
        if (!IsConnected || _serialPort == null)
            return false;
        try
        {
            await Task.Run(() => _serialPort.Write(data, 0, data.Length));
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"USB Print failed: {ex.Message}");
            return false;
        }
#elif ANDROID
        if (!IsConnected || _usbConnection == null || _outEndpoint == null)
            return false;

        try
        {
            int result = await Task.Run(() => _usbConnection.BulkTransfer(_outEndpoint, data, data.Length, 5000));
            return result >= 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"USB Print failed: {ex.Message}");
            return false;
        }
#else
        await Task.CompletedTask;
        return false;
#endif
    }
}

// ─── UsbPermissionReceiver (Android only) ─────────────────────────

#if ANDROID
/// <summary>
/// Receives the USB permission grant/deny result and resolves the awaited TaskCompletionSource.
/// </summary>
internal class UsbPermissionReceiver : BroadcastReceiver
{
    private readonly string _deviceId;
    private readonly TaskCompletionSource<bool> _tcs;

    public UsbPermissionReceiver(string deviceId, TaskCompletionSource<bool> tcs)
    {
        _deviceId = deviceId;
        _tcs      = tcs;
    }

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent == null) return;

        var device = (UsbDevice?)intent.GetParcelableExtra(UsbManager.ExtraDevice);
        if (device == null || device.DeviceName != _deviceId) return;

        bool granted = intent.GetBooleanExtra(UsbManager.ExtraPermissionGranted, false);
        _tcs.TrySetResult(granted);
    }
}
#endif
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using InTheHand.Net;
#if ANDROID
using Android.Bluetooth;
#endif

namespace rendezvous_companion.Services;

public class BluetoothPrinterService : IPrinterService
{
    private BluetoothClient? _bluetoothClient;
    private Stream? _dataStream;

    public bool IsConnected => _bluetoothClient?.Connected ?? false;

    // ─── GetAvailableDevicesAsync ──────────────────────────────────

    public async Task<List<PrinterDevice>> GetAvailableDevicesAsync()
    {
        return await Task.Run(() =>
        {
            var devices = new List<PrinterDevice>();

            try
            {
                if (BluetoothRadio.Default == null || BluetoothRadio.Default.Mode == RadioMode.PowerOff)
                    return devices;

                var client = new BluetoothClient();

                // 1. Get Paired Devices
                var pairedDevices = client.PairedDevices;
                foreach (var dev in pairedDevices)
                {
                    devices.Add(new PrinterDevice
                    {
                        Id       = dev.DeviceAddress.ToString(),
                        Name     = dev.DeviceName + " (Paired)",
                        ConnectionType = PrinterConnectionType.Bluetooth,
                        IsPaired = true
                    });
                }

                // 2. Discover New Devices
                var discoveredDevices = client.DiscoverDevices();
                foreach (var dev in discoveredDevices)
                {
                    if (!devices.Any(d => d.Id == dev.DeviceAddress.ToString()))
                    {
                        devices.Add(new PrinterDevice
                        {
                            Id       = dev.DeviceAddress.ToString(),
                            Name     = dev.DeviceName,
                            ConnectionType = PrinterConnectionType.Bluetooth,
                            IsPaired = false
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BT scan failed or not supported: {ex.Message}");
            }

            return devices;
        });
    }

    // ─── PairAsync ────────────────────────────────────────────────
    // Initiates an OS-level bond with the device. On Android this shows
    // the system pairing dialog. On other platforms it's a no-op.

    public async Task<bool> PairAsync(string deviceId)
    {
#if ANDROID
        return await Task.Run(async () =>
        {
            try
            {
                var adapter = BluetoothAdapter.DefaultAdapter;
                if (adapter == null) return false;

                // Convert the colon-free MAC (e.g. "001122334455") to colon format
                var mac = FormatMac(deviceId);
                var device = adapter.GetRemoteDevice(mac);
                if (device == null) return false;

                // Already bonded — nothing to do
                if (device.BondState == Bond.Bonded) return true;

                // Use a TaskCompletionSource to wait for the bond result
                var tcs = new TaskCompletionSource<bool>();

                var receiver = new BondStateReceiver(mac, tcs);
                Android.App.Application.Context.RegisterReceiver(
                    receiver,
                    new Android.Content.IntentFilter(BluetoothDevice.ActionBondStateChanged)
                );

                device.CreateBond();

                // Wait up to 30 seconds for pairing to complete
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                cts.Token.Register(() => tcs.TrySetResult(false));

                var result = await tcs.Task;

                try { Android.App.Application.Context.UnregisterReceiver(receiver); }
                catch { /* already unregistered */ }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BT PairAsync failed: {ex.Message}");
                return false;
            }
        });
#else
        // On Windows the InTheHand library handles pairing internally;
        // treat the device as already paired.
        await Task.CompletedTask;
        return true;
#endif
    }

    // ─── UnpairOthersAsync ────────────────────────────────────────
    // Removes the OS-level bond from every paired BT device except the
    // one we want to keep. Uses Java reflection because removeBond() is
    // a hidden (but stable) Android SDK method.

    public async Task UnpairOthersAsync(string keepDeviceId)
    {
#if ANDROID
        await Task.Run(() =>
        {
            try
            {
                var adapter = BluetoothAdapter.DefaultAdapter;
                if (adapter?.BondedDevices == null) return;

                var keepMac = FormatMac(keepDeviceId);

                foreach (var bonded in adapter.BondedDevices)
                {
                    if (bonded?.Address == null) continue;
                    if (string.Equals(bonded.Address, keepMac, StringComparison.OrdinalIgnoreCase))
                        continue;

                    try
                    {
                        // removeBond() is hidden; use Java reflection to access it
                        var javaClass = Java.Lang.Class.FromType(typeof(BluetoothDevice));
                        var removeBondMethod = javaClass.GetDeclaredMethod("removeBond");
                        removeBondMethod?.Invoke(bonded);
                        Console.WriteLine($"[BT] Unpaired: {bonded.Address}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[BT] Failed to unpair {bonded.Address}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BT] UnpairOthersAsync failed: {ex.Message}");
            }
        });
#else
        await Task.CompletedTask;
#endif
    }

    // ─── ConnectAsync ─────────────────────────────────────────────

    public async Task<bool> ConnectAsync(string deviceId)
    {
        try
        {
            var address = BluetoothAddress.Parse(deviceId);
            _bluetoothClient = new BluetoothClient();

            await Task.Run(() =>
            {
                _bluetoothClient.Connect(address, BluetoothService.SerialPort);
                _dataStream = _bluetoothClient.GetStream();
            });

            return IsConnected;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"BT Connection failed: {ex.Message}");
            return false;
        }
    }

    // ─── DisconnectAsync ──────────────────────────────────────────

    public async Task DisconnectAsync()
    {
        await Task.Run(() =>
        {
            _dataStream?.Close();
            _dataStream?.Dispose();
            _bluetoothClient?.Close();
            _bluetoothClient?.Dispose();
            _dataStream = null;
            _bluetoothClient = null;
        });
    }

    // ─── PrintAsync ───────────────────────────────────────────────

    public async Task<bool> PrintAsync(byte[] data)
    {
        if (!IsConnected || _dataStream == null)
            return false;

        try
        {
            await _dataStream.WriteAsync(data, 0, data.Length);
            await _dataStream.FlushAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"BT Print failed: {ex.Message}");
            return false;
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Converts a compact MAC address (001122334455 or 00:11:22:33:44:55)
    /// to the Android colon-separated format (00:11:22:33:44:55).
    /// </summary>
    private static string FormatMac(string id)
    {
        // Remove existing colons/dashes
        var clean = id.Replace(":", "").Replace("-", "");
        if (clean.Length == 12)
            return string.Join(":", Enumerable.Range(0, 6).Select(i => clean.Substring(i * 2, 2)));
        return id; // already in some usable format
    }
}

#if ANDROID
/// <summary>
/// BroadcastReceiver that resolves a TaskCompletionSource when the
/// target device's bond state changes to Bonded or None.
/// </summary>
internal class BondStateReceiver : Android.Content.BroadcastReceiver
{
    private readonly string _mac;
    private readonly TaskCompletionSource<bool> _tcs;

    public BondStateReceiver(string mac, TaskCompletionSource<bool> tcs)
    {
        _mac = mac;
        _tcs = tcs;
    }

    public override void OnReceive(Android.Content.Context? context, Android.Content.Intent? intent)
    {
        if (intent?.Action != BluetoothDevice.ActionBondStateChanged) return;

        var device = (BluetoothDevice?)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
        if (device == null || !string.Equals(device.Address, _mac, StringComparison.OrdinalIgnoreCase))
            return;

        var newState = (Bond)(intent.GetIntExtra(BluetoothDevice.ExtraBondState, (int)Bond.None));

        if (newState == Bond.Bonded)
            _tcs.TrySetResult(true);
        else if (newState == Bond.None)
            _tcs.TrySetResult(false);
        // Bond.Bonding = still in progress, wait
    }
}
#endif
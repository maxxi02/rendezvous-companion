using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using InTheHand.Net;

namespace rendezvous_companion.Services;

public class BluetoothPrinterService : IPrinterService
{
    private BluetoothClient? _bluetoothClient;
    private Stream? _dataStream;

    public bool IsConnected => _bluetoothClient?.Connected ?? false;

    public async Task<List<PrinterDevice>> GetAvailableDevicesAsync()
    {
        return await Task.Run(() =>
        {
            var devices = new List<PrinterDevice>();
            var client = new BluetoothClient();

            // 1. Get Paired Devices
            var pairedDevices = client.PairedDevices;
            foreach (var dev in pairedDevices)
            {
                devices.Add(new PrinterDevice
                {
                    Id = dev.DeviceAddress.ToString(),
                    Name = dev.DeviceName + " (Paired)",
                    ConnectionType = PrinterConnectionType.Bluetooth
                });
            }

            // 2. Discover New Devices
            var discoveredDevices = client.DiscoverDevices();
            foreach (var dev in discoveredDevices)
            {
                // Only add if not already in the list (from paired)
                if (!devices.Any(d => d.Id == dev.DeviceAddress.ToString()))
                {
                    devices.Add(new PrinterDevice
                    {
                        Id = dev.DeviceAddress.ToString(),
                        Name = dev.DeviceName,
                        ConnectionType = PrinterConnectionType.Bluetooth
                    });
                }
            }

            return devices;
        });
    }

    public async Task<bool> ConnectAsync(string deviceId)
    {
        try
        {
            var address = BluetoothAddress.Parse(deviceId);
            _bluetoothClient = new BluetoothClient();

            await Task.Run(() =>
            {
                // Serial Port Profile (SPP) UUID is the standard for Classic BT printers
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
}
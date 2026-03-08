using System.IO.Ports;
# if ANDROID
using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Microsoft.Maui.ApplicationModel;
# endif

namespace rendezvous_companion.Services;

public class UsbPrinterService : IPrinterService
{
# if WINDOWS
    private SerialPort? _serialPort;
# endif
# if ANDROID
    private UsbDevice? _usbDevice;
    private UsbDeviceConnection? _usbConnection;
    private UsbEndpoint? _outEndpoint;
# endif

    public bool IsConnected =>
# if WINDOWS
        _serialPort?.IsOpen ?? false;
# elif ANDROID
        _usbConnection != null;
# else
        false;
# endif

    public async Task<List<PrinterDevice>> GetAvailableDevicesAsync()
    {
        var devices = new List<PrinterDevice>();

# if WINDOWS
        await Task.Run(() =>
        {
            try
            {
                var ports = SerialPort.GetPortNames();
                foreach (var port in ports)
                {
                    devices.Add(new PrinterDevice
                    {
                        Id = port,
                        Name = port,
                        ConnectionType = PrinterConnectionType.USB
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"USB scan error: {ex.Message}");
            }
        });
# elif ANDROID
        await Task.Run(() =>
        {
            var usbManager = (UsbManager?)Android.App.Application.Context.GetSystemService(Context.UsbService);
            if (usbManager?.DeviceList?.Values == null) return;

            foreach (var device in usbManager.DeviceList.Values)
            {
                // Basic filter: look for printers or common vendor IDs if known, 
                // but usually, we just list them if they have a bulk out endpoint.
                devices.Add(new PrinterDevice
                {
                    Id = device.DeviceName,
                    Name = $"{device.ProductName ?? "USB Device"} ({device.VendorId}:{device.ProductId})",
                    ConnectionType = PrinterConnectionType.USB
                });
            }
        });
# else
        await Task.CompletedTask;
# endif
        return devices;
    }

    public async Task<bool> ConnectAsync(string deviceId)
    {
# if WINDOWS
        try
        {
            await Task.Run(() =>
            {
                _serialPort = new SerialPort(deviceId)
                {
                    BaudRate = 9600,
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    ReadTimeout = 500,
                    WriteTimeout = 500
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
# elif ANDROID
        try
        {
            var usbManager = (UsbManager?)Android.App.Application.Context.GetSystemService(Context.UsbService);
            if (usbManager?.DeviceList?.Values == null) return false;

            _usbDevice = usbManager.DeviceList.Values.FirstOrDefault(d => d.DeviceName == deviceId);
            if (_usbDevice == null) return false;

            // Simple permission check (in a real app, you might need a BroadcastReceiver for permission results)
            if (!usbManager.HasPermission(_usbDevice))
            {
                // This will trigger the system popup
                // Note: ConnectAsync might need to be retried by the user after they click OK
#pragma warning disable CA1416
                var flags = Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M 
                    ? PendingIntentFlags.Immutable 
                    : 0;
                var intent = PendingIntent.GetBroadcast(Android.App.Application.Context, 0, new Intent("com.solarworks.USB_PERMISSION"), flags);
                usbManager.RequestPermission(_usbDevice, intent);
#pragma warning restore CA1416
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
# else
        await Task.CompletedTask;
        return false;
# endif
    }

    public async Task DisconnectAsync()
    {
# if WINDOWS
        await Task.Run(() =>
        {
            _serialPort?.Close();
            _serialPort?.Dispose();
            _serialPort = null;
        });
# elif ANDROID
        await Task.Run(() =>
        {
            _usbConnection?.Close();
            _usbConnection?.Dispose();
            _usbConnection = null;
            _outEndpoint = null;
            _usbDevice = null;
        });
# else
        await Task.CompletedTask;
# endif
    }

    public async Task<bool> PrintAsync(byte[] data)
    {
# if WINDOWS
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
# elif ANDROID
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
# else
        await Task.CompletedTask;
        return false;
# endif
    }
}
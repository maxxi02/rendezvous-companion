namespace rendezvous_companion.Services;

public interface IPrinterService
{
    Task<bool> ConnectAsync(string deviceId);
    Task DisconnectAsync();
    Task<bool> PrintAsync(byte[] data);
    Task<List<PrinterDevice>> GetAvailableDevicesAsync();

    /// <summary>Initiate OS-level pairing (BT bond / USB permission request).</summary>
    Task<bool> PairAsync(string deviceId);

    /// <summary>Remove OS pairing / close connections for every device except keepDeviceId.</summary>
    Task UnpairOthersAsync(string keepDeviceId);

    bool IsConnected { get; }
}

public class PrinterDevice
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PrinterConnectionType ConnectionType { get; set; }

    /// <summary>True when the device is already bonded (BT) or permission granted (USB).</summary>
    public bool IsPaired { get; set; }
}

public enum PrinterConnectionType
{
    USB,
    Bluetooth
}
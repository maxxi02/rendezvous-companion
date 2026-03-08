namespace rendezvous_companion.Services;

public interface IPrinterService
{
    Task<bool> ConnectAsync(string deviceId);
    Task DisconnectAsync();
    Task<bool> PrintAsync(byte[] data);
    Task<List<PrinterDevice>> GetAvailableDevicesAsync();
    bool IsConnected { get; }
}

public class PrinterDevice
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PrinterConnectionType ConnectionType { get; set; }
}

public enum PrinterConnectionType
{
    USB,
    Bluetooth
}
using rendezvous_companion.Models;

namespace rendezvous_companion.Services;

/// <summary>
/// Persists printer device selections across app restarts.
/// </summary>
public static class DevicePreferencesService
{
    private const string ReceiptIdKey = "receipt_printer_id";
    private const string ReceiptNameKey = "receipt_printer_name";
    private const string ReceiptTypeKey = "receipt_printer_type";
    private const string KitchenIdKey = "kitchen_printer_id";
    private const string KitchenNameKey = "kitchen_printer_name";
    private const string KitchenTypeKey = "kitchen_printer_type";

    public static void SaveReceiptPrinter(PrinterDevice device)
    {
        Preferences.Set(ReceiptIdKey, device.Id);
        Preferences.Set(ReceiptNameKey, device.Name);
        Preferences.Set(ReceiptTypeKey, (int)device.ConnectionType);
    }

    public static void SaveKitchenPrinter(PrinterDevice device)
    {
        Preferences.Set(KitchenIdKey, device.Id);
        Preferences.Set(KitchenNameKey, device.Name);
        Preferences.Set(KitchenTypeKey, (int)device.ConnectionType);
    }

    public static PrinterDevice? LoadReceiptPrinter()
    {
        var id = Preferences.Get(ReceiptIdKey, string.Empty);
        if (string.IsNullOrEmpty(id)) return null;
        return new PrinterDevice
        {
            Id = id,
            Name = Preferences.Get(ReceiptNameKey, "Receipt Printer"),
            ConnectionType = (PrinterConnectionType)Preferences.Get(ReceiptTypeKey, 0),
            IsPaired = true,
        };
    }

    public static PrinterDevice? LoadKitchenPrinter()
    {
        var id = Preferences.Get(KitchenIdKey, string.Empty);
        if (string.IsNullOrEmpty(id)) return null;
        return new PrinterDevice
        {
            Id = id,
            Name = Preferences.Get(KitchenNameKey, "Kitchen Printer"),
            ConnectionType = (PrinterConnectionType)Preferences.Get(KitchenTypeKey, 0),
            IsPaired = true,
        };
    }

    public static void ClearReceiptPrinter()
    {
        Preferences.Remove(ReceiptIdKey);
        Preferences.Remove(ReceiptNameKey);
        Preferences.Remove(ReceiptTypeKey);
    }

    public static void ClearKitchenPrinter()
    {
        Preferences.Remove(KitchenIdKey);
        Preferences.Remove(KitchenNameKey);
        Preferences.Remove(KitchenTypeKey);
    }
}

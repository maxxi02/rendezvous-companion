using System.Text;

namespace rendezvous_companion.Templates;

/// <summary>
/// ESC/POS command builder for 58mm thermal printers (Xprinter XP-58IIH)
/// 384 dots per line, ~32 chars normal, ~16 chars large
/// </summary>
public static class EscPosCommands
{
    // ESC/POS command bytes
    public static readonly byte[] Initialize        = { 0x1B, 0x40 };
    public static readonly byte[] NewLine           = { 0x0A };
    public static readonly byte[] CutPaper          = { 0x1D, 0x56, 0x41, 0x10 };
    public static readonly byte[] AlignLeft         = { 0x1B, 0x61, 0x00 };
    public static readonly byte[] AlignCenter       = { 0x1B, 0x61, 0x01 };
    public static readonly byte[] AlignRight        = { 0x1B, 0x61, 0x02 };
    public static readonly byte[] BoldOn            = { 0x1B, 0x45, 0x01 };
    public static readonly byte[] BoldOff           = { 0x1B, 0x45, 0x00 };
    public static readonly byte[] DoubleHeightOn    = { 0x1B, 0x21, 0x10 };
    public static readonly byte[] DoubleWidthOn     = { 0x1B, 0x21, 0x20 };
    public static readonly byte[] LargeFontOn       = { 0x1B, 0x21, 0x30 }; // double height + width
    public static readonly byte[] NormalFont        = { 0x1B, 0x21, 0x00 };
    public static readonly byte[] FeedLines3        = { 0x1B, 0x64, 0x03 };

    public static byte[] Text(string text)
        => Encoding.ASCII.GetBytes(text);

    public static byte[] Line(string text)
        => Combine(Text(text), NewLine);

    public static byte[] Divider()
        => Line("--------------------------------");

    public static byte[] ShortDivider()
        => Line("- - - - - - - - - - - - - - - -");

    /// <summary>
    /// Format an order item line: "Burger           1x  P150.00"
    /// Total line width = 32 chars for 58mm
    /// </summary>
    public static byte[] OrderItemLine(string name, int qty, decimal price)
    {
        string priceStr = $"P{price:F2}";
        string qtyStr = $"{qty}x";
        int nameMaxLen = 32 - qtyStr.Length - priceStr.Length - 2;
        string namePadded = name.Length > nameMaxLen
            ? name[..nameMaxLen]
            : name.PadRight(nameMaxLen);
        return Line($"{namePadded} {qtyStr} {priceStr}");
    }

    /// <summary>
    /// Format a total line: "TOTAL            P330.00"
    /// </summary>
    public static byte[] TotalLine(string label, decimal amount)
    {
        string amountStr = $"P{amount:F2}";
        string labelPadded = label.PadRight(32 - amountStr.Length);
        return Line($"{labelPadded}{amountStr}");
    }

    public static byte[] Combine(params byte[][] arrays)
    {
        var result = new List<byte>();
        foreach (var arr in arrays)
            result.AddRange(arr);
        return result.ToArray();
    }

    /// <summary>
    /// Generates ESC/POS commands to print a QR code.
    /// </summary>
    public static byte[] QRCode(string data)
    {
        var strBytes = Encoding.ASCII.GetBytes(data);
        int pL = (strBytes.Length + 3) & 0xFF;
        int pH = ((strBytes.Length + 3) >> 8) & 0xFF;

        var storeDataCmd = new byte[] { 0x1D, 0x28, 0x6B, (byte)pL, (byte)pH, 0x31, 0x50, 0x30 };
        
        return Combine(
            new byte[] { 0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00 }, // Model 2
            new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, 0x06 },       // Size 6
            new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x30 },       // Error correction L
            storeDataCmd,
            strBytes,
            new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30 }        // Print
        );
    }
}
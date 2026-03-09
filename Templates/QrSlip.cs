using static rendezvous_companion.Templates.EscPosCommands;

namespace rendezvous_companion.Templates;

public static class QrSlip
{
    public static byte[] Build(string url, string label)
    {
        var parts = new List<byte[]>
        {
            Initialize,
            AlignCenter,
            BoldOn,
            LargeFontOn,
            Line(label),
            NormalFont,
            BoldOff,
            NewLine,
            Line("Scan to order"),
            NewLine,
            QRCode(url),
            NewLine,
            NewLine,
            Line(url),
            FeedLines3,
            CutPaper
        };

        return Combine(parts.ToArray());
    }
}

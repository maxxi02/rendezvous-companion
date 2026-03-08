using rendezvous_companion.Models;
using static rendezvous_companion.Templates.EscPosCommands;

namespace rendezvous_companion.Templates;

public static class KitchenSlip
{
    public static byte[] Build(Order order)
    {
        var parts = new List<byte[]>
        {
            Initialize,

            // Header - large so cook can read fast
            AlignCenter,
            BoldOn,
            LargeFontOn,
            Line("** KITCHEN **"),
            NormalFont,
            BoldOff,
            NewLine,

            AlignLeft,
            BoldOn,
            Line($"ORDER #: {order.OrderNumber}"),
            BoldOff,
            Line($"Time : {order.OrderDate:h:mm tt}"),
            Line($"Table: {order.TableNumber}"),

            Divider(),
        };

        // Print each item with large text so kitchen can read easily
        foreach (var item in order.Items)
        {
            parts.AddRange(new[]
            {
                LargeFontOn,
                BoldOn,
                Line($"[{item.Quantity}x] {item.Name}"),
                NormalFont,
                BoldOff,
            });

            // Print notes/special instructions if any
            if (!string.IsNullOrEmpty(item.Notes))
            {
                parts.Add(Line($"  >> {item.Notes}"));
            }

            parts.Add(NewLine);
        }

        parts.AddRange(new[]
        {
            Divider(),
            AlignCenter,
            Line($"--- END OF ORDER ---"),
            FeedLines3,
            CutPaper
        });

        return Combine(parts.ToArray());
    }
}
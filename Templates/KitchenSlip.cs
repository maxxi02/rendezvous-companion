using rendezvous_companion.Models;
using static rendezvous_companion.Templates.EscPosCommands;

namespace rendezvous_companion.Templates;

public static class KitchenSlip
{
    public static byte[] Build(Order order)
    {
        // Only print food items on the kitchen slip
        var foodItems = order
            .Items.Where(i =>
                string.IsNullOrEmpty(i.MenuType)
                || i.MenuType.Equals("food", StringComparison.OrdinalIgnoreCase)
            )
            .ToList();

        // Skip building the slip if there's nothing to cook
        if (foodItems.Count == 0)
            return Array.Empty<byte>();

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
        };

        if (!string.IsNullOrEmpty(order.TableNumber))
            parts.Add(Line($"Table: {order.TableNumber}"));

        if (!string.IsNullOrEmpty(order.OrderNote))
        {
            parts.Add(Divider());
            parts.Add(BoldOn);
            parts.Add(Line($"NOTE: {order.OrderNote}"));
            parts.Add(BoldOff);
        }

        parts.Add(Divider());

        // Print each food item with large text so kitchen can read easily
        foreach (var item in foodItems)
        {
            parts.AddRange(
                new[]
                {
                    LargeFontOn,
                    BoldOn,
                    Line($"[{item.Quantity}x] {item.Name}"),
                    NormalFont,
                    BoldOff,
                }
            );

            // Print notes/special instructions if any
            if (!string.IsNullOrEmpty(item.Notes))
            {
                parts.Add(Line($"  >> {item.Notes}"));
            }

            parts.Add(NewLine);
        }

        parts.AddRange(
            new[] { Divider(), AlignCenter, Line($"--- END OF ORDER ---"), FeedLines3, CutPaper }
        );

        return Combine(parts.ToArray());
    }
}

using rendezvous_companion.Models;
using static rendezvous_companion.Templates.EscPosCommands;

namespace rendezvous_companion.Templates;

public static class KitchenSlip
{
    public static byte[] Build(Order order)
    {
        var foodItems = order.Items
            .Where(i => i.IsCookable)
            .ToList();

        if (foodItems.Count == 0)
            return Array.Empty<byte>();

        // Convert UTC → local time
        var localDate = order.OrderDate.Kind == DateTimeKind.Utc
            ? order.OrderDate.ToLocalTime()
            : order.OrderDate;

        var orderType = order.OrderType?.ToLower() switch
        {
            "dine-in"  => "Dine-in",
            "takeout"  => "Take Away",
            "takeaway" => "Take Away",
            _          => "Dine-in",
        };

        var parts = new List<byte[]>
        {
            Initialize,
            AlignCenter,
            BoldOn,
            LargeFontOn,
            Line("KITCHEN"),
            NormalFont,
            BoldOff,
            NewLine,
            AlignLeft,
            BoldOn,
            Line($"ORDER #: {order.OrderNumber}"),
            BoldOff,
            Line($"Time : {localDate:h:mm tt}"),
            Line($"Type : {orderType}"),
        };

        if (!string.IsNullOrEmpty(order.TableNumber))
            parts.Add(Line($"Table: {order.TableNumber}"));

        // Only print customer name if present
        if (!string.IsNullOrEmpty(order.CustomerName))
            parts.Add(Line($"Name : {order.CustomerName}"));

        if (!string.IsNullOrEmpty(order.OrderNote))
        {
            parts.Add(Divider());
            parts.Add(BoldOn);
            parts.Add(Line($"NOTE: {order.OrderNote}"));
            parts.Add(BoldOff);
        }

        parts.Add(Divider());

        foreach (var item in foodItems)
        {
            parts.AddRange(new[]
            {
                LargeFontOn,
                BoldOn,
                Line($"[{item.Quantity}x] {item.Name}"),
                NormalFont,
                BoldOff,
            });

            if (!string.IsNullOrEmpty(item.Notes))
            {
                parts.Add(BoldOn);
                parts.Add(Line($"  >> {item.Notes}"));
                parts.Add(BoldOff);
            }

            if (item.Addons != null && item.Addons.Count > 0)
            {
                parts.Add(DoubleHeightOn);
                parts.Add(BoldOn);
                foreach (var addon in item.Addons)
                {
                    parts.Add(Line($"  + {addon.AddonName}"));
                }
                parts.Add(NormalFont);
                parts.Add(BoldOff);
            }

            parts.Add(NewLine);
        }

        parts.AddRange(new[] { Divider(), AlignCenter, Line("--- END OF ORDER ---"), FeedLines3, CutPaper });

        return Combine(parts.ToArray());
    }
}

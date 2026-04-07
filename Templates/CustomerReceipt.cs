using rendezvous_companion.Models;
using static rendezvous_companion.Templates.EscPosCommands;

namespace rendezvous_companion.Templates;

public static class CustomerReceipt
{
    public static byte[] Build(Order order)
    {
        var storeName    = !string.IsNullOrEmpty(order.BusinessName) ? order.BusinessName : "RENDEZVOUS";
        var storeAddress = order.BusinessAddress;
        var storeTel     = order.BusinessPhone;
        var receiptMsg   = order.ReceiptMessage;

        // Convert UTC → local time for accurate timestamp
        var localDate = order.OrderDate.Kind == DateTimeKind.Utc
            ? order.OrderDate.ToLocalTime()
            : order.OrderDate;

        var parts = new List<byte[]> { Initialize };

        // REPRINT banner
        if (order.IsReprint)
            parts.AddRange(new[] { AlignCenter, BoldOn, Line("*** REPRINT ***"), BoldOff });

        // Business logo from settings
        if (!string.IsNullOrEmpty(order.BusinessLogo))
            parts.Add(Base64Image(order.BusinessLogo));

        // Store name — normal bold, no large font
        parts.AddRange(new[]
        {
            AlignCenter,
            BoldOn,
            Line(storeName),
            BoldOff,
        });

        if (!string.IsNullOrEmpty(storeAddress))
            parts.Add(Line(storeAddress));

        if (!string.IsNullOrEmpty(storeTel))
            parts.Add(Line($"Tel: {storeTel}"));

        parts.AddRange(new[]
        {
            NewLine,
            AlignLeft,
            Divider(),
            Line($"Order #: {order.OrderNumber}"),
            Line($"Date   : {localDate:MM/dd/yyyy h:mm tt}"),
        });

        // Order type
        var orderType = order.OrderType?.ToLower() switch
        {
            "dine-in"  => "Dine-in",
            "takeout"  => "Take Away",
            "takeaway" => "Take Away",
            _          => "Dine-in",
        };
        parts.Add(Line($"Type   : {orderType}"));

        if (!string.IsNullOrEmpty(order.TableNumber))
            parts.Add(Line($"Table  : {order.TableNumber}"));

        if (!string.IsNullOrEmpty(order.Cashier))
            parts.Add(Line($"Cashier: {order.Cashier}"));

        // Only print customer name if present
        if (!string.IsNullOrEmpty(order.CustomerName))
            parts.Add(Line($"Name   : {order.CustomerName}"));

        parts.Add(Divider());

        // Items
        foreach (var item in order.Items)
        {
            var effectivePrice = item.HasDiscount ? item.Price * 0.8m : item.Price;
            parts.Add(OrderItemLine(item.Name, item.Quantity, effectivePrice));
            if (item.HasDiscount)
                parts.Add(Line("  [20% Senior/PWD Discount]"));
        }

        parts.AddRange(new[]
        {
            Divider(),
            TotalLine("Subtotal", order.Subtotal),
            TotalLine("Discount", order.DiscountTotal),
            BoldOn,
            TotalLine("TOTAL", order.Total),
            BoldOff,
            Divider(),
        });

        // Payment method
        var method = order.PaymentMethod?.ToLower() ?? "cash";

        if (method == "split" && order.SplitPayment != null)
        {
            parts.Add(Line("Payment Method:"));
            parts.Add(TotalLine("  Cash:", order.SplitPayment.Cash));
            parts.Add(TotalLine("  GCash:", order.SplitPayment.Gcash));
            parts.Add(Divider());
            parts.Add(BoldOn);
            parts.Add(TotalLine("Total:", order.Total));
            parts.Add(BoldOff);
            var splitChange = order.AmountPaid > order.Total ? order.AmountPaid - order.Total : 0m;
            parts.Add(TotalLine("Change:", splitChange));
        }
        else if (method == "gcash")
        {
            parts.Add(Line("Payment Method:"));
            parts.Add(TotalLine("  GCash:", order.Total));
            parts.Add(Divider());
            parts.Add(BoldOn);
            parts.Add(TotalLine("Total:", order.Total));
            parts.Add(BoldOff);
        }
        else
        {
            parts.Add(Line("Payment Method:"));
            parts.Add(TotalLine("  Cash:", order.AmountPaid));
            parts.Add(Divider());
            parts.Add(BoldOn);
            parts.Add(TotalLine("Total:", order.Total));
            parts.Add(BoldOff);
            parts.Add(TotalLine("Change:", order.Change));
        }

        parts.Add(Divider());

        parts.Add(AlignCenter);
        parts.Add(NewLine);
        
        if (!string.IsNullOrEmpty(receiptMsg))
            parts.Add(Line(receiptMsg));
        else
        {
            parts.Add(Line("Thank you for dining with us!"));
            parts.Add(Line("Please come again :)"));
        }

        if (!string.IsNullOrEmpty(order.Disclaimer))
        {
            parts.Add(Line(order.Disclaimer));
        }

        parts.Add(NewLine);
        parts.Add(FeedLines3);
        parts.Add(CutPaper);

        return Combine(parts.ToArray());
    }
}

using rendezvous_companion.Models;
using static rendezvous_companion.Templates.EscPosCommands;

namespace rendezvous_companion.Templates;

public static class CustomerReceipt
{
    public static byte[] Build(Order order)
    {
        var storeName = !string.IsNullOrEmpty(order.BusinessName)
            ? order.BusinessName
            : "RENDEZVOUS";
        var storeAddress = order.BusinessAddress;
        var storeTel = order.BusinessPhone;
        var receiptMessage = order.ReceiptMessage;
        var parts = new List<byte[]>
        {
            // Initialize printer
            Initialize,
        };

        if (!string.IsNullOrEmpty(order.BusinessLogo))
        {
            parts.Add(Base64Image(order.BusinessLogo));
        }

        parts.AddRange(
            new[]
            {
                // Store header
                AlignCenter,
                BoldOn,
                LargeFontOn,
                Line(storeName),
                NormalFont,
                BoldOff,
            }
        );

        if (!string.IsNullOrEmpty(storeAddress))
            parts.Add(Line(storeAddress));

        if (!string.IsNullOrEmpty(storeTel))
            parts.Add(Line($"Tel: {storeTel}"));

        parts.AddRange(
            new[]
            {
                NewLine,
                AlignLeft,
                Divider(),
                // Order info
                Line($"Order #: {order.OrderNumber}"),
                Line($"Date   : {order.OrderDate:MM/dd/yyyy h:mm tt}"),
            }
        );

        if (!string.IsNullOrEmpty(order.TableNumber))
            parts.Add(Line($"Table  : {order.TableNumber}"));

        if (!string.IsNullOrEmpty(order.Cashier))
            parts.Add(Line($"Cashier: {order.Cashier}"));

        if (!string.IsNullOrEmpty(order.CustomerName))
            parts.Add(Line($"Name   : {order.CustomerName}"));

        parts.Add(Divider());

        // Order items
        foreach (var item in order.Items)
            parts.Add(OrderItemLine(item.Name, item.Quantity, item.Price));

        parts.AddRange(
            new[]
            {
                Divider(),
                // Totals
                TotalLine("Subtotal", order.Subtotal),
                TotalLine("Discount", order.DiscountTotal),
                BoldOn,
                TotalLine("TOTAL", order.Total),
                BoldOff,
                Divider(),
            }
        );

        // ── Payment Method section ──────────────────────────────────
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
            // Change = any cash overpayment (usually 0 for split)
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
            // Cash
            parts.Add(Line("Payment Method:"));
            parts.Add(TotalLine("  Cash:", order.AmountPaid));
            parts.Add(Divider());
            parts.Add(BoldOn);
            parts.Add(TotalLine("Total:", order.Total));
            parts.Add(BoldOff);
            parts.Add(TotalLine("Change:", order.Change));
        }

        parts.Add(Divider());

        parts.AddRange(
            new[]
            {
                Divider(),
                // Footer
                AlignCenter,
                NewLine,
                !string.IsNullOrEmpty(receiptMessage)
                    ? Line(receiptMessage)
                    : Line("Thank you for dining with us!"),
                Line("Please come again :)"),
                NewLine,
                // Feed and cut
                FeedLines3,
                CutPaper,
            }
        );

        return Combine(parts.ToArray());
    }
}

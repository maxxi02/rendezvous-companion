using rendezvous_companion.Models;
using static rendezvous_companion.Templates.EscPosCommands;

namespace rendezvous_companion.Templates;

public static class CustomerReceipt
{
    public static byte[] Build(Order order)
    {
        var storeName = !string.IsNullOrEmpty(order.BusinessName) ? order.BusinessName : "RENDEZVOUS";
        var storeAddress = order.BusinessAddress;
        var storeTel = order.BusinessPhone;
        var receiptMessage = order.ReceiptMessage;
        var parts = new List<byte[]>
        {
            // Initialize printer
            Initialize,

            // Store header
            AlignCenter,
            BoldOn,
            LargeFontOn,
            Line(storeName),
            NormalFont,
            BoldOff,
        };

        if (!string.IsNullOrEmpty(storeAddress))
            parts.Add(Line(storeAddress));

        if (!string.IsNullOrEmpty(storeTel))
            parts.Add(Line($"Tel: {storeTel}"));

        parts.AddRange(new[]
        {
            NewLine,
            AlignLeft,
            Divider(),

            // Order info
            Line($"Order #: {order.OrderNumber}"),
            Line($"Date   : {order.OrderDate:MM/dd/yyyy h:mm tt}"),
        });

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

        parts.AddRange(new[]
        {
            Divider(),

            // Totals
            TotalLine("Subtotal", order.Subtotal),
            TotalLine("Discount", order.DiscountTotal),
            TotalLine("VAT (12%)", order.Tax),
            BoldOn,
            TotalLine("TOTAL", order.Total),
            BoldOff,
            NewLine,
            TotalLine($"Paid via {order.PaymentMethod}", order.AmountPaid),
            TotalLine("Change", order.Change),

            Divider(),

            // Footer
            AlignCenter,
            NewLine,
            !string.IsNullOrEmpty(receiptMessage) ? Line(receiptMessage) : Line("Thank you for dining with us!"),
            Line("Please come again :)"),
            NewLine,
            Line($"VAT Reg TIN: 000-000-000-000"),
            NewLine,

            // Feed and cut
            FeedLines3,
            CutPaper
        });

        return Combine(parts.ToArray());
    }
}
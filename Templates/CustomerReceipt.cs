using rendezvous_companion.Models;
using static rendezvous_companion.Templates.EscPosCommands;

namespace rendezvous_companion.Templates;

public static class CustomerReceipt
{
    // Helper: check if a section should appear in a specific position
    // Falls back to default behavior if sections config is null
    private static bool ShowInHeader(ReceiptSectionConfig? cfg, bool defaultVal = true)
        => cfg == null ? defaultVal : (cfg.Header && !cfg.Disabled);

    private static bool ShowInFooter(ReceiptSectionConfig? cfg, bool defaultVal = false)
        => cfg == null ? defaultVal : (cfg.Footer && !cfg.Disabled);

    private static bool IsDisabled(ReceiptSectionConfig? cfg)
        => cfg?.Disabled == true;

    public static byte[] Build(Order order)
    {
        var sections   = order.Sections;
        var receiptMsg = order.ReceiptMessage;

        // Convert UTC → local time for accurate timestamp
        var localDate = order.OrderDate.Kind == DateTimeKind.Utc
            ? order.OrderDate.ToLocalTime()
            : order.OrderDate;

        var parts = new List<byte[]> { Initialize };

        // ── REPRINT banner ─────────────────────────────────────────
        if (order.IsReprint)
            parts.AddRange(new[] { AlignCenter, BoldOn, Line("REPRINT"), BoldOff });

        // ── HEADER ────────────────────────────────────────────────
        parts.Add(AlignCenter);

        // Business logo
        if (!string.IsNullOrEmpty(order.BusinessLogo))
            parts.Add(Base64Image(order.BusinessLogo));

        // Store name in header (default: shown in header)
        if (ShowInHeader(sections?.StoreName))
        {
            parts.Add(BoldOn);
            parts.Add(Line(order.BusinessName));
            parts.Add(BoldOff);
        }

        // Location address in header (default: shown in header)
        if (ShowInHeader(sections?.LocationAddress) && !string.IsNullOrEmpty(order.BusinessAddress))
            parts.Add(Line(order.BusinessAddress));

        // Phone number (default: NOT in header — must be explicitly enabled)
        if (ShowInHeader(sections?.PhoneNumber, false) && !string.IsNullOrEmpty(order.BusinessPhone))
            parts.Add(Line($"Tel: {order.BusinessPhone}"));

        // Receipt message in header only if explicitly set as header
        if (ShowInHeader(sections?.Message, false) && !string.IsNullOrEmpty(receiptMsg))
            parts.Add(Line(receiptMsg));

        // ── ORDER Meta ─────────────────────────────────────────────
        parts.AddRange(new[]
        {
            NewLine,
            AlignLeft,
            Divider(),
            Line($"Order #: {order.OrderNumber}"),
            Line($"Date   : {localDate:MM/dd/yyyy h:mm tt}"),
        });

        // Order type (if not disabled)
        if (!IsDisabled(sections?.OrderType))
        {
            var orderType = order.OrderType?.ToLower() switch
            {
                "dine-in"  => "Dine-in",
                "takeout"  => "Take Away",
                "takeaway" => "Take Away",
                _          => "Dine-in",
            };
            parts.Add(Line($"Type   : {orderType}"));
        }

        if (!string.IsNullOrEmpty(order.TableNumber))
            parts.Add(Line($"Table  : {order.TableNumber}"));

        if (!string.IsNullOrEmpty(order.Cashier))
            parts.Add(Line($"Cashier: {order.Cashier}"));

        // Customer name (if not disabled via customerInfo section)
        if (!IsDisabled(sections?.CustomerInfo) && !string.IsNullOrEmpty(order.CustomerName))
            parts.Add(Line($"Name   : {order.CustomerName}"));

        // Order note in header (only if explicitly set to header position)
        if (ShowInHeader(sections?.OrderNote, false) && !string.IsNullOrEmpty(order.OrderNote))
        {
            parts.Add(Divider());
            parts.Add(BoldOn);
            parts.Add(Line($"NOTE: {order.OrderNote}"));
            parts.Add(BoldOff);
        }

        parts.Add(Divider());

        // ── ITEMS ─────────────────────────────────────────────────
        foreach (var item in order.Items)
        {
            var effectivePrice = item.HasDiscount ? item.Price * 0.8m : item.Price;
            parts.Add(OrderItemLine(item.Name, item.Quantity, effectivePrice));
            if (item.HasDiscount)
                parts.Add(Line("  [20% Senior/PWD Discount]"));
            foreach (var addon in item.Addons)
            {
                var addonLabel = addon.Price > 0
                    ? $"  + {addon.AddonName} (P{addon.Price:F2})"
                    : $"  + {addon.AddonName}";
                parts.Add(Line(addonLabel));
            }
        }

        // ── TOTALS ─────────────────────────────────────────────────
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

        // ── PAYMENT METHOD ─────────────────────────────────────────
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

        // ── FOOTER ────────────────────────────────────────────────
        parts.Add(AlignCenter);
        parts.Add(NewLine);

        // Order note in footer (default position for orderNote)
        if (ShowInFooter(sections?.OrderNote, true) && !string.IsNullOrEmpty(order.OrderNote))
        {
            parts.Add(BoldOn);
            parts.Add(Line($"NOTE: {order.OrderNote}"));
            parts.Add(BoldOff);
        }

        // Store name in footer (only if explicitly set to footer)
        if (ShowInFooter(sections?.StoreName, false))
        {
            parts.Add(BoldOn);
            parts.Add(Line(order.BusinessName));
            parts.Add(BoldOff);
        }

        // Address in footer
        if (ShowInFooter(sections?.LocationAddress, false) && !string.IsNullOrEmpty(order.BusinessAddress))
            parts.Add(Line(order.BusinessAddress));

        // Phone in footer
        if (ShowInFooter(sections?.PhoneNumber, false) && !string.IsNullOrEmpty(order.BusinessPhone))
            parts.Add(Line($"Tel: {order.BusinessPhone}"));

        // Receipt message (default: footer)
        if (ShowInFooter(sections?.Message, true) && !string.IsNullOrEmpty(receiptMsg))
            parts.Add(Line(receiptMsg));
        else if (sections == null)
        {
            // Fallback when no sections config at all
            parts.Add(Line("Thank you for dining with us!"));
            parts.Add(Line("Please come again :)"));
        }

        // Disclaimer
        if (!IsDisabled(sections?.Disclaimer) && !string.IsNullOrEmpty(order.Disclaimer))
            parts.Add(Line(order.Disclaimer));

        parts.Add(NewLine);
        parts.Add(FeedLines3);
        parts.Add(CutPaper);

        return Combine(parts.ToArray());
    }
}

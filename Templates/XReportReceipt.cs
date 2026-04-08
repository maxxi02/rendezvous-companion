using rendezvous_companion.Models;
using static rendezvous_companion.Templates.EscPosCommands;

namespace rendezvous_companion.Templates;

/// <summary>
/// X-Reading Report — mid-session summary without closing the register.
/// Intentionally simpler than Z-Report: no BALANCED/SHORT/OVER, no signatures,
/// no accumulated sales. Just a quick snapshot of today's activity so far.
/// </summary>
public static class XReportReceipt
{
    private const int LineLength = 32;

    public static byte[] Build(ZReport report)
    {
        var parts = new List<byte[]>
        {
            Initialize,
            AlignCenter,
        };

        // ── Header ───────────────────────────────────────────────────
        parts.Add(BoldOn);
        parts.Add(Line(report.BusinessName ?? "Business Name"));
        parts.Add(BoldOff);

        if (!string.IsNullOrEmpty(report.LocationAddress))
            parts.Add(Line(report.LocationAddress));

        if (!string.IsNullOrEmpty(report.TaxPin))
            parts.Add(Line($"TIN: {report.TaxPin}"));

        parts.Add(Divider());
        parts.Add(BoldOn);
        parts.Add(Line("X-READING REPORT"));
        parts.Add(BoldOff);
        parts.Add(Divider());

        // ── Session Info ─────────────────────────────────────────────
        parts.Add(AlignLeft);
        parts.Add(FormatLine("Date:", report.Today));
        parts.Add(FormatLine("Time:", report.TimeNow));
        parts.Add(FormatLine(
            "Cashier:",
            string.IsNullOrEmpty(report.CashierName) ? "—" : report.CashierName
        ));
        parts.Add(FormatLine(
            "Register:",
            string.IsNullOrEmpty(report.RegisterName) ? "—" : report.RegisterName
        ));
        parts.Add(FormatLine(
            "Opened:",
            string.IsNullOrEmpty(report.OpenedAt) ? "—" : report.OpenedAt
        ));

        parts.Add(Divider());

        // ── Sales Summary ────────────────────────────────────────────
        parts.Add(AlignCenter);
        parts.Add(Line("SALES SUMMARY"));
        parts.Add(AlignLeft);

        parts.Add(FormatLine("Transactions:", report.Transactions.ToString()));
        parts.Add(FormatLine("Total Items:", report.Items.ToString()));
        parts.Add(Divider_Thin());

        parts.Add(FormatLine("Gross Sales:", FormatMoney(report.TotalSales)));
        parts.Add(FormatLine("Discounts:", "-" + FormatMoney(report.TotalDiscounts)));

        if (report.TotalRefunds > 0)
            parts.Add(FormatLine("Returns:", "-" + FormatMoney(report.TotalRefunds)));

        if (report.TotalVoids > 0)
            parts.Add(FormatLine("Voids:", "-" + FormatMoney(report.TotalVoids)));

        parts.Add(Divider());
        parts.Add(BoldOn);
        parts.Add(FormatLine("NET SALES:", FormatMoney(report.NetSales)));
        parts.Add(BoldOff);
        parts.Add(Divider());

        // ── Cash in Drawer ───────────────────────────────────────────
        parts.Add(AlignCenter);
        parts.Add(Line("CASH IN DRAWER"));
        parts.Add(AlignLeft);

        parts.Add(FormatLine("Opening Fund:", FormatMoney(report.OpeningFund)));
        parts.Add(FormatLine("Cash Sales:", FormatMoney(report.CashEarned)));

        if (report.GCashEarned > 0)
            parts.Add(FormatLine("GCash Sales:", FormatMoney(report.GCashEarned)));

        if (report.CashOuts > 0)
            parts.Add(FormatLine("Payouts:", "-" + FormatMoney(report.CashOuts)));

        if (report.TotalRefunds > 0)
            parts.Add(FormatLine("Cash Refunds:", "-" + FormatMoney(report.TotalRefunds)));

        parts.Add(Divider());
        parts.Add(BoldOn);
        parts.Add(FormatLine("EXPECTED CASH:", FormatMoney(report.ExpectedCash)));
        parts.Add(BoldOff);
        parts.Add(Divider());

        // ── Payment Breakdown ────────────────────────────────────────
        parts.Add(AlignCenter);
        parts.Add(Line("PAYMENT BREAKDOWN"));
        parts.Add(AlignLeft);

        if (report.Tenders != null && report.Tenders.Count > 0)
        {
            var tenderLabels = new Dictionary<string, string>
            {
                { "cash",        "CASH" },
                { "gcash",       "GCASH" },
                { "split",       "SPLIT" },
                { "credit_card", "CREDIT CARD" },
                { "pay_later",   "PAY LATER" },
                { "online",      "ONLINE" },
                { "invoice",     "INVOICE" },
                { "e_wallet",    "EWALLET" },
                { "pay_in",      "PAYIN" },
            };

            foreach (var kvp in report.Tenders)
            {
                if (kvp.Value <= 0) continue;
                var label = tenderLabels.TryGetValue(kvp.Key, out string? tlbl)
                    ? tlbl
                    : kvp.Key.ToUpper();
                parts.Add(FormatLine($"{label}:", FormatMoney(kvp.Value)));
            }
        }
        else
        {
            parts.Add(FormatLine("CASH:", FormatMoney(report.CashEarned)));
        }

        parts.Add(Divider());

        // ── Discounts Detail ─────────────────────────────────────────
        if (report.Discounts != null && report.Discounts.Count > 0)
        {
            parts.Add(AlignCenter);
            parts.Add(Line("DISCOUNTS"));
            parts.Add(AlignLeft);

            foreach (var d in report.Discounts)
                parts.Add(FormatLine(d.Label, FormatMoney(d.Value)));

            parts.Add(Divider());
        }

        // ── Footer ───────────────────────────────────────────────────
        parts.Add(AlignCenter);

        if (!string.IsNullOrEmpty(report.ReceiptMessage))
        {
            parts.Add(Line(report.ReceiptMessage));
            parts.Add(Line(""));
        }

        if (!string.IsNullOrEmpty(report.Disclaimer))
            parts.Add(Line(report.Disclaimer));
        else
            parts.Add(Line("POS System Receipt"));

        parts.Add(BoldOn);
        parts.Add(Line("END OF X-REPORT"));
        parts.Add(BoldOff);

        parts.Add(FeedLines3);
        parts.Add(CutPaper);

        return Combine(parts.ToArray());
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static byte[] FormatLine(string label, string value)
    {
        if (label.Length + value.Length >= LineLength)
        {
            int availableLabelSpace = Math.Max(0, LineLength - value.Length - 1);
            string truncLabel =
                label.Length > availableLabelSpace
                    ? label.Substring(0, availableLabelSpace)
                    : label;
            return Line($"{truncLabel} {value}".PadRight(LineLength));
        }

        return Line($"{label}{value.PadLeft(LineLength - label.Length)}");
    }

    private static string FormatMoney(double amount) => amount.ToString("N2");

    /// <summary>Thin dotted separator line for sub-sections within a section.</summary>
    private static byte[] Divider_Thin() => Line("................................");
}

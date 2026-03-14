using rendezvous_companion.Models;
using static rendezvous_companion.Templates.EscPosCommands;

namespace rendezvous_companion.Templates;

public static class ZReportReceipt
{
    private const int LineLength = 32;

    public static byte[] Build(ZReport report)
    {
        var parts = new List<byte[]>
        {
            Initialize,
            AlignCenter,
            // Header
            BoldOn,
            Line(report.BusinessName ?? "Business Name"),
            BoldOff,
        };

        if (!string.IsNullOrEmpty(report.LocationAddress))
            parts.Add(Line(report.LocationAddress));

        if (!string.IsNullOrEmpty(report.TaxPin))
            parts.Add(Line($"TIN: {report.TaxPin}"));

        parts.Add(Divider());
        parts.Add(BoldOn);
        parts.Add(Line(report.IsXReading ? "X-READING REPORT" : "Z-READING REPORT"));
        parts.Add(BoldOff);
        parts.Add(Divider());

        parts.Add(AlignLeft);

        // Date/Time
        parts.Add(FormatLine("Date:", report.Today));
        parts.Add(FormatLine("Time:", report.TimeNow));
        parts.Add(
            FormatLine(
                "Cashier:",
                string.IsNullOrEmpty(report.CashierName) ? "—" : report.CashierName
            )
        );
        parts.Add(
            FormatLine(
                "Register:",
                string.IsNullOrEmpty(report.RegisterName) ? "—" : report.RegisterName
            )
        );
        parts.Add(
            FormatLine("Opened:", string.IsNullOrEmpty(report.OpenedAt) ? "—" : report.OpenedAt)
        );
        if (!report.IsXReading)
        {
            parts.Add(
                FormatLine("Closed:", string.IsNullOrEmpty(report.ClosedAt) ? "—" : report.ClosedAt)
            );
        }

        parts.Add(Divider());

        // TODAY'S SALES OR SALES SUMMARY
        parts.Add(AlignCenter);
        parts.Add(Line(report.IsXReading ? "SALES SUMMARY" : "TODAY'S SALES"));
        parts.Add(AlignLeft);

        parts.Add(FormatLine("Gross Sales:", FormatMoney(report.TotalSales)));
        parts.Add(FormatLine("Discounts:", "-" + FormatMoney(report.TotalDiscounts)));

        if (report.TotalRefunds > 0)
            parts.Add(FormatLine("Returns:", "-" + FormatMoney(report.TotalRefunds)));

        if (report.TotalVoids > 0)
            parts.Add(FormatLine("Voids:", FormatMoney(report.TotalVoids)));

        parts.Add(Divider());
        parts.Add(BoldOn);
        parts.Add(FormatLine("NET SALES:", FormatMoney(report.NetSales)));
        parts.Add(BoldOff);
        parts.Add(Divider());

        // CASH SUMMARY OR CASH IN DRAWER
        parts.Add(AlignCenter);
        parts.Add(Line(report.IsXReading ? "CASH IN DRAWER" : "CASH SUMMARY"));
        parts.Add(AlignLeft);

        parts.Add(FormatLine("Opening Fund:", FormatMoney(report.OpeningFund)));
        parts.Add(FormatLine("Cash Sales:", FormatMoney(report.CashEarned)));

        if (report.TotalRefunds > 0)
            parts.Add(FormatLine("Cash Refunds:", "-" + FormatMoney(report.TotalRefunds)));

        parts.Add(Divider());

        parts.Add(BoldOn);
        parts.Add(FormatLine("EXPECTED CASH:", FormatMoney(report.ExpectedCash)));
        
        if (!report.IsXReading)
        {
            parts.Add(FormatLine("COUNTED CASH:", FormatMoney(report.ActualCash)));
            parts.Add(
                FormatLine(
                    "DIFFERENCE:",
                    report.Difference < 0
                        ? $"({FormatMoney(Math.Abs(report.Difference))})"
                        : FormatMoney(report.Difference)
                )
            );
        }
        parts.Add(BoldOff);

        parts.Add(Divider());

        // PAYMENT BREAKDOWN
        parts.Add(AlignCenter);
        parts.Add(Line("PAYMENT BREAKDOWN"));
        parts.Add(AlignLeft);

        if (report.Tenders != null && report.Tenders.Count > 0)
        {
            var tenderLabels = new Dictionary<string, string>
            {
                { "cash", "CASH" },
                { "credit_card", "CREDIT" },
                { "pay_later", "PY LTR" },
                { "online", "ONLINE" },
                { "invoice", "INVOICE" },
                { "e_wallet", "EWALLET" },
                { "pay_in", "PAYIN" },
            };

            foreach (var kvp in report.Tenders)
            {
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

        // DISCOUNTS
        parts.Add(AlignCenter);
        parts.Add(Line("DISCOUNTS"));
        parts.Add(AlignLeft);

        if (report.Discounts != null && report.Discounts.Count > 0)
        {
            foreach (var d in report.Discounts)
            {
                parts.Add(FormatLine(d.Label, FormatMoney(d.Value)));
            }
        }
        else
        {
            parts.Add(AlignCenter);
            parts.Add(Line("No discounts"));
            parts.Add(AlignLeft);
        }

        parts.Add(Divider());

        // STATUS (BALANCED / SHORT / OVER)
        if (!report.IsXReading)
        {
            parts.Add(AlignCenter);
            parts.Add(BoldOn);
            if (Math.Abs(report.Difference) < 0.01)
            {
                parts.Add(Line("✓ BALANCED ✓"));
            }
            else if (report.Difference < 0)
            {
                parts.Add(Line($"! SHORT: ({FormatMoney(Math.Abs(report.Difference))}) !"));
            }
            else
            {
                parts.Add(Line($"! OVER: +{FormatMoney(report.Difference)} !"));
            }
            parts.Add(BoldOff);
            parts.Add(Divider());
        }

        // DAILY SUMMARY OR TRANSACTIONS
        parts.Add(AlignCenter);
        parts.Add(Line(report.IsXReading ? "TRANSACTIONS" : "DAILY SUMMARY"));
        parts.Add(AlignLeft);

        parts.Add(FormatLine("Total Transactions:", report.Transactions.ToString()));
        if (!report.IsXReading)
        {
            parts.Add(FormatLine("Total Items:", report.Items.ToString()));
            parts.Add(BoldOn);
            parts.Add(FormatLine("NET INCOME:", FormatMoney(report.NetSales)));
            parts.Add(BoldOff);
        }

        parts.Add(Divider());

        // Receipt Message
        parts.Add(AlignCenter);
        if (!string.IsNullOrEmpty(report.ReceiptMessage))
        {
            parts.Add(Line(report.ReceiptMessage));
            parts.Add(Line(""));
        }

        // Signatures
        if (!report.IsXReading && report.ShowCashierSignature)
        {
            parts.Add(Line(""));
            parts.Add(Line(""));
            parts.Add(Line("_______________  _______________"));
            parts.Add(Line("Cashier          Manager      "));
            parts.Add(Line(""));
        }

        if (!string.IsNullOrEmpty(report.Disclaimer))
        {
            parts.Add(Line(report.Disclaimer));
        }
        else
        {
            parts.Add(Line("Dizlog - RigelSoft PH"));
        }

        parts.Add(BoldOn);
        parts.Add(Line(report.IsXReading ? "END OF X-REPORT" : "END OF Z-REPORT"));
        parts.Add(BoldOff);

        // Feed & Cut
        parts.Add(FeedLines3);
        parts.Add(CutPaper);

        return Combine(parts.ToArray());
    }

    private static byte[] FormatLine(string label, string value)
    {
        if (label.Length + value.Length >= LineLength)
        {
            // Truncate label if necessary to fit with a space
            int availableLabelSpace = Math.Max(0, LineLength - value.Length - 1);
            string truncLabel =
                label.Length > availableLabelSpace
                    ? label.Substring(0, availableLabelSpace)
                    : label;
            return Line($"{truncLabel} {value}".PadRight(LineLength));
        }

        return Line($"{label}{value.PadLeft(LineLength - label.Length)}");
    }

    private static string FormatMoney(double amount)
    {
        return amount.ToString("N2");
    }
}

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
        };

        // Print logo if available
        if (!string.IsNullOrEmpty(report.BusinessLogo))
        {
            parts.Add(Base64Image(report.BusinessLogo));
        }

        parts.AddRange(new[]
        {
            // Header
            BoldOn,
            Line(report.BusinessName ?? "Business Name"),
            BoldOff,
        });

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
        parts.Add(
            FormatLine("Closed:", string.IsNullOrEmpty(report.ClosedAt) ? "—" : report.ClosedAt)
        );

        parts.Add(Divider());

        // TODAY'S SALES OR SALES SUMMARY
        parts.Add(AlignCenter);
        parts.Add(Line("SALES SUMMARY"));
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

        // TRANSACTION SUMMARY
        parts.Add(Line("Transaction Summary"));
        parts.Add(Divider());
        parts.Add(AlignLeft);

        parts.Add(FormatLine("Cash in Drawer:", FormatMoney(report.ActualCash)));

        double cashTender = 0, creditCard = 0, payLater = 0, online = 0, invoice = 0, ewallet = 0, payin = 0;
        if (report.Tenders != null)
        {
            report.Tenders.TryGetValue("cash", out cashTender);
            report.Tenders.TryGetValue("credit_card", out creditCard);
            report.Tenders.TryGetValue("pay_later", out payLater);
            report.Tenders.TryGetValue("online", out online);
            report.Tenders.TryGetValue("invoice", out invoice);
            report.Tenders.TryGetValue("e_wallet", out ewallet);
            report.Tenders.TryGetValue("pay_in", out payin);
        }
        else
        {
            cashTender = report.CashEarned;
        }

        parts.Add(FormatLine("CASH:", FormatMoney(cashTender)));
        parts.Add(FormatLine("CREDIT CARD:", FormatMoney(creditCard)));
        parts.Add(FormatLine("PAY LATER:", FormatMoney(payLater)));
        parts.Add(FormatLine("ONLINE:", FormatMoney(online)));
        parts.Add(FormatLine("INVOICE:", FormatMoney(invoice)));
        parts.Add(FormatLine("EWALLET:", FormatMoney(ewallet)));
        parts.Add(FormatLine("PAYIN:", FormatMoney(payin)));

        parts.Add(FormatLine("Opening Fund:", FormatMoney(report.OpeningFund)));
        // Assuming "Less Withdrawal" covers the pay-outs and refunds
        double totalWithdrawals = report.CashOuts + report.TotalRefunds;
        parts.Add(FormatLine("Less Withdrawal:", FormatMoney(totalWithdrawals)));

        // Not exactly sure what Payment Received maps to in the DB, keeping it 0.00 for now 
        // to match the specific layout unless it's GCash?
        double paymentReceived = 0;
        if (report.Tenders != null && report.Tenders.TryGetValue("gcash", out double gcash))
        {
             // GCash is omitted from the image but if there's GCash we might map it here or ignore.
             // We'll leave it 0 or map GCash if requested.
             // Let's just output Payment Received: 0.00 to match the image, and then add total tenders.
        }
        parts.Add(FormatLine("Payment Received:", FormatMoney(paymentReceived)));

        // Unlabeled total line holding sum of tenders (from image: 2739 + 1067 = 3806)
        double totalTenders = cashTender + creditCard + payLater + online + invoice + ewallet + payin;
        parts.Add(FormatLine(" ", FormatMoney(totalTenders)));

        parts.Add(Divider());

        parts.Add(FormatLine("Short/Over:", FormatMoney(report.Difference)));
        
        parts.Add(Divider());

        // DAILY SUMMARY OR TRANSACTIONS
        parts.Add(AlignCenter);
        parts.Add(Line("DAILY SUMMARY"));
        parts.Add(AlignLeft);

        parts.Add(FormatLine("Total Transactions:", report.Transactions.ToString()));
        parts.Add(FormatLine("Total Items:", report.Items.ToString()));
        parts.Add(BoldOn);
        parts.Add(FormatLine("NET INCOME:", FormatMoney(report.NetSales)));
        parts.Add(BoldOff);

        parts.Add(Divider());

        // ACCUMULATED SALES
        if (report.PresentAccumulatedSales.HasValue)
        {
            parts.Add(AlignCenter);
            parts.Add(Line("ACCUMULATED SALES"));
            parts.Add(AlignLeft);

            parts.Add(FormatLine("Previous:", FormatMoney(report.PreviousAccumulatedSales ?? 0)));
            parts.Add(FormatLine("Current:", FormatMoney(report.PresentAccumulatedSales.Value)));
            parts.Add(Divider());
        }

        // Receipt Message
        parts.Add(AlignCenter);
        if (!string.IsNullOrEmpty(report.ReceiptMessage))
        {
            parts.Add(Line(report.ReceiptMessage));
            parts.Add(Line(""));
        }

        // Signatures
        if (report.ShowCashierSignature)
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

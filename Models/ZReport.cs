using System.Text.Json.Serialization;

namespace rendezvous_companion.Models;

public class ZReport
{
    [JsonPropertyName("businessName")]
    public string BusinessName { get; set; } = "";

    [JsonPropertyName("locationAddress")]
    public string LocationAddress { get; set; } = "";

    [JsonPropertyName("taxPin")]
    public string TaxPin { get; set; } = "";

    [JsonPropertyName("today")]
    public string Today { get; set; } = "";

    [JsonPropertyName("timeNow")]
    public string TimeNow { get; set; } = "";

    [JsonPropertyName("cashierName")]
    public string CashierName { get; set; } = "";

    [JsonPropertyName("registerName")]
    public string RegisterName { get; set; } = "";

    [JsonPropertyName("openedAt")]
    public string OpenedAt { get; set; } = "";

    [JsonPropertyName("closedAt")]
    public string ClosedAt { get; set; } = "";

    [JsonPropertyName("totalSales")]
    public double TotalSales { get; set; }

    [JsonPropertyName("totalDiscounts")]
    public double TotalDiscounts { get; set; }

    [JsonPropertyName("totalRefunds")]
    public double TotalRefunds { get; set; }

    [JsonPropertyName("totalVoids")]
    public double TotalVoids { get; set; }

    [JsonPropertyName("netSales")]
    public double NetSales { get; set; }

    [JsonPropertyName("openingFund")]
    public double OpeningFund { get; set; }

    [JsonPropertyName("cashEarned")]
    public double CashEarned { get; set; }

    [JsonPropertyName("expectedCash")]
    public double ExpectedCash { get; set; }

    [JsonPropertyName("actualCash")]
    public double ActualCash { get; set; }

    [JsonPropertyName("difference")]
    public double Difference { get; set; }

    [JsonPropertyName("tenders")]
    public Dictionary<string, double>? Tenders { get; set; }

    [JsonPropertyName("discounts")]
    public List<ZReportDiscount>? Discounts { get; set; }

    [JsonPropertyName("transactions")]
    public int Transactions { get; set; }

    [JsonPropertyName("items")]
    public int Items { get; set; }

    [JsonPropertyName("receiptMessage")]
    public string ReceiptMessage { get; set; } = "";

    [JsonPropertyName("disclaimer")]
    public string Disclaimer { get; set; } = "";

    [JsonPropertyName("showCashierSignature")]
    public bool ShowCashierSignature { get; set; }

    [JsonPropertyName("isXReading")]
    public bool IsXReading { get; set; }
}

public class ZReportDiscount
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    [JsonPropertyName("value")]
    public double Value { get; set; }
}

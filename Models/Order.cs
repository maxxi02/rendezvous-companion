using System.Text.Json.Serialization;

namespace rendezvous_companion.Models;

public class Order
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    // POS sends "timestamp" for new orders (print:request path)
    // This overwrites OrderDate if provided (takes priority over "createdAt")
    // Always treat incoming dates as UTC so ToLocalTime() works correctly.
    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp
    {
        set
        {
            if (value.HasValue)
            {
                var dt = value.Value;
                OrderDate = dt.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                    : dt;
            }
        }
    }

    [JsonPropertyName("tableNumber")]
    public string TableNumber { get; set; } = string.Empty;

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<OrderItem> Items { get; set; } = new();

    [JsonPropertyName("orderType")]
    public string OrderType { get; set; } = "dine-in";

    [JsonPropertyName("orderNote")]
    public string OrderNote { get; set; } = string.Empty;

    [JsonPropertyName("cashier")]
    public string Cashier { get; set; } = string.Empty;

    // Payment
    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; set; }

    [JsonPropertyName("discountTotal")]
    public decimal DiscountTotal { get; set; }

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("amountPaid")]
    public decimal AmountPaid { get; set; }

    [JsonPropertyName("change")]
    public decimal Change { get; set; }

    [JsonPropertyName("paymentMethod")]
    public string PaymentMethod { get; set; } = "cash";

    [JsonPropertyName("splitPayment")]
    public SplitPayment? SplitPayment { get; set; }

    [JsonPropertyName("paymentStatus")]
    public string PaymentStatus { get; set; } = "pending";

    [JsonPropertyName("seniorPwdCount")]
    public int? SeniorPwdCount { get; set; }

    // Store Info (Database-driven from POS)
    [JsonPropertyName("businessName")]
    public string BusinessName { get; set; } = string.Empty;

    [JsonPropertyName("businessLogo")]
    public string BusinessLogo { get; set; } = string.Empty;

    [JsonPropertyName("businessAddress")]
    public string BusinessAddress { get; set; } = string.Empty;

    [JsonPropertyName("businessPhone")]
    public string BusinessPhone { get; set; } = string.Empty;

    [JsonPropertyName("receiptMessage")]
    public string ReceiptMessage { get; set; } = string.Empty;

    [JsonPropertyName("disclaimer")]
    public string Disclaimer { get; set; } = string.Empty;

    // Reprint flag (sent from POS when reprinting an existing receipt)
    [JsonPropertyName("isReprint")]
    public bool IsReprint { get; set; } = false;

    // Receipt sections config (from POS receipt settings)
    [JsonPropertyName("sections")]
    public ReceiptSections? Sections { get; set; }

    // Queue
    [JsonPropertyName("queueStatus")]
    public string QueueStatus { get; set; } = "pending_payment";

    public decimal Tax => Subtotal * 0.12m; // 12% VAT
}

public class ReceiptSectionConfig
{
    [JsonPropertyName("header")]
    public bool Header { get; set; } = true;

    [JsonPropertyName("footer")]
    public bool Footer { get; set; } = false;

    [JsonPropertyName("disabled")]
    public bool Disabled { get; set; } = false;
}

public class ReceiptSections
{
    [JsonPropertyName("storeName")]
    public ReceiptSectionConfig? StoreName { get; set; }

    [JsonPropertyName("locationAddress")]
    public ReceiptSectionConfig? LocationAddress { get; set; }

    [JsonPropertyName("phoneNumber")]
    public ReceiptSectionConfig? PhoneNumber { get; set; }

    [JsonPropertyName("message")]
    public ReceiptSectionConfig? Message { get; set; }

    [JsonPropertyName("disclaimer")]
    public ReceiptSectionConfig? Disclaimer { get; set; }

    [JsonPropertyName("orderType")]
    public ReceiptSectionConfig? OrderType { get; set; }

    [JsonPropertyName("customerInfo")]
    public ReceiptSectionConfig? CustomerInfo { get; set; }

    [JsonPropertyName("orderNote")]
    public ReceiptSectionConfig? OrderNote { get; set; }
}

public class OrderItem
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;

    [JsonPropertyName("hasDiscount")]
    public bool HasDiscount { get; set; } = false;

    [JsonPropertyName("menuType")]
    public string MenuType { get; set; } = "food"; // "food" or "drink"

    [JsonPropertyName("isCookable")]
    public bool IsCookable { get; set; } = false;

    [JsonPropertyName("addons")]
    public List<OrderAddon> Addons { get; set; } = new();

    public decimal TotalSum => Quantity * Price;
}

public class OrderAddon
{
    [JsonPropertyName("addonName")]
    public string AddonName { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }
}

public class SplitPayment
{
    [JsonPropertyName("cash")]
    public decimal Cash { get; set; }

    [JsonPropertyName("gcash")]
    public decimal Gcash { get; set; }
}

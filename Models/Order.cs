using System.Text.Json.Serialization;

namespace rendezvous_companion.Models;

public class Order
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime OrderDate { get; set; } = DateTime.Now;

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

    [JsonPropertyName("paymentStatus")]
    public string PaymentStatus { get; set; } = "pending";

    [JsonPropertyName("seniorPwdCount")]
    public int? SeniorPwdCount { get; set; }
    
    // Queue
    [JsonPropertyName("queueStatus")]
    public string QueueStatus { get; set; } = "pending_payment";

    public decimal Tax => Subtotal * 0.12m; // 12% VAT
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

    [JsonPropertyName("menuType")]
    public string MenuType { get; set; } = "food"; // "food" or "drink"

    public decimal TotalSum => Quantity * Price;
}
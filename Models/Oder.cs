namespace rendezvous_companion.Models;

public class Order
{
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public int TableNumber { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
    public decimal CashReceived { get; set; }
    public string PaymentMethod { get; set; } = "Cash";

    public decimal Subtotal => Items.Sum(i => i.Total);
    public decimal Tax => Subtotal * 0.12m; // 12% VAT
    public decimal Total => Subtotal + Tax;
    public decimal Change => CashReceived - Total;
}

public class OrderItem
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Notes { get; set; } = string.Empty;
    public decimal Total => Quantity * UnitPrice;
}
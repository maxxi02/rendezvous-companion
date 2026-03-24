using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace rendezvous_companion.Models
{
    public class Order
    {
        [JsonPropertyName(""orderId"")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName(""orderNumber"")]
        public string OrderNumber { get; set; } = string.Empty;

        [JsonPropertyName(""createdAt"")]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [JsonPropertyName(""tableNumber"")]
        public string TableNumber { get; set; } = string.Empty;

        [JsonPropertyName(""customerName"")]
        public string CustomerName { get; set; } = string.Empty;

        [JsonPropertyName(""items"")]
        public List<OrderItem> Items { get; set; } = new();

        [JsonPropertyName(""orderType"")]
        public string OrderType { get; set; } = ""dine-in"";

        [JsonPropertyName(""orderNote"")]
        public string OrderNote { get; set; } = string.Empty;

        [JsonPropertyName(""cashier"")]
        public string Cashier { get; set; } = string.Empty;

        [JsonPropertyName(""subtotal"")]
        public decimal Subtotal { get; set; }

        [JsonPropertyName(""discountTotal"")]
        public decimal DiscountTotal { get; set; }

        [JsonPropertyName(""total"")]
        public decimal Total { get; set; }

        [JsonPropertyName(""amountPaid"")]
        public decimal AmountPaid { get; set; }

        [JsonPropertyName(""change"")]
        public decimal Change { get; set; }

        [JsonPropertyName(""paymentMethod"")]
        public string PaymentMethod { get; set; } = ""cash"";

        [JsonPropertyName(""paymentStatus"")]
        public string PaymentStatus { get; set; } = ""pending"";

        [JsonPropertyName(""seniorPwdCount"")]
        public int? SeniorPwdCount { get; set; }

        [JsonPropertyName(""businessName"")]
        public string BusinessName { get; set; } = string.Empty;

        [JsonPropertyName(""businessLogo"")]
        public string BusinessLogo { get; set; } = string.Empty;

        [JsonPropertyName(""businessAddress"")]
        public string BusinessAddress { get; set; } = string.Empty;

        [JsonPropertyName(""businessPhone"")]
        public string BusinessPhone { get; set; } = string.Empty;

        [JsonPropertyName(""receiptMessage"")]
        public string ReceiptMessage { get; set; } = string.Empty;

        [JsonPropertyName(""queueStatus"")]
        public string QueueStatus { get; set; } = ""pending_payment"";

        public decimal Tax => Subtotal * 0.12m;
    }

    public class OrderItem
    {
        [JsonPropertyName(""_id"")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName(""name"")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName(""quantity"")]
        public int Quantity { get; set; }

        [JsonPropertyName(""price"")]
        public decimal Price { get; set; }

        [JsonPropertyName(""notes"")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName(""menuType"")]
        public string MenuType { get; set; } = ""food"";

        public decimal TotalSum => Quantity * Price;
    }
}

namespace TestApp
{
    using rendezvous_companion.Models;

    class Program
    {
        static void Main(string[] args)
        {
            var json = @"{
                ""orderNumber"": ""ORD-123"",
                ""customerName"": ""John Doe"",
                ""cashier"": ""Cashier 1"",
                ""timestamp"": ""2026-03-24T12:00:00.000Z"",
                ""orderType"": ""dine-in"",
                ""tableNumber"": ""5"",
                ""items"": [
                    { ""name"": ""Burger"", ""price"": 150.00, ""quantity"": 2, ""hasDiscount"": false }
                ],
                ""subtotal"": 300.00,
                ""discountTotal"": 0,
                ""total"": 300.00,
                ""paymentMethod"": ""cash"",
                ""splitPayment"": { ""cash"": 0, ""gcash"": 0 },
                ""amountPaid"": 500.00,
                ""change"": 200.00,
                ""seniorPwdIds"": [],
                ""isReprint"": true,
                ""businessName"": ""Test Business"",
                ""businessAddress"": ""123 Test St"",
                ""businessPhone"": ""1234567890"",
                ""businessLogo"": null,
                ""receiptMessage"": ""Thank you!""
            }";

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var order = JsonSerializer.Deserialize<Order>(json, options);
                
                if (order != null)
                {
                    Console.WriteLine(""Success: Parsed Order for "" + order.BusinessName);
                    Console.WriteLine(""Total: "" + order.Total);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(""Error parsing Order: "" + ex.Message);
            }
        }
    }
}

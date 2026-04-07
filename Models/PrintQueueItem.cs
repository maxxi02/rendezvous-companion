using System.Text.Json.Serialization;

namespace rendezvous_companion.Models;

public enum PrintJobType { Receipt, Kitchen, QR, ZReport, XReport }
public enum PrintJobStatus { Pending, Failed, Printed }

public class PrintQueueItem
{
    public string JobId { get; set; } = Guid.NewGuid().ToString();
    public PrintJobType JobType { get; set; }
    public PrintJobStatus Status { get; set; } = PrintJobStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int RetryCount { get; set; } = 0;
    public string OrderId { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;

    // Serialized payload — either Order JSON or ZReport JSON
    public string Payload { get; set; } = string.Empty;

    // For QR jobs
    public string? QrUrl { get; set; }
    public string? QrLabel { get; set; }
    public string? QrTarget { get; set; }

    [JsonIgnore]
    public string StatusLabel => Status switch
    {
        PrintJobStatus.Pending => "Pending",
        PrintJobStatus.Failed => $"Failed ({RetryCount}x)",
        PrintJobStatus.Printed => "Printed",
        _ => "Unknown"
    };

    [JsonIgnore]
    public Color StatusColor => Status switch
    {
        PrintJobStatus.Printed => Color.FromArgb("#28a745"),
        PrintJobStatus.Failed => Color.FromArgb("#DC3545"),
        _ => Color.FromArgb("#fd7e14"),
    };
}

namespace OrderDesk.Web.Models;

public sealed class WebhookReceipt
{
    public int Id { get; set; }
    public string EventId { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}

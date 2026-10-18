namespace OrderDesk.Web.Models;

public sealed class OpportunityWebhookReceipt
{
    public int Id { get; set; }
    public string EventId { get; set; } = string.Empty;
    public int OpportunityId { get; set; }
    public Opportunity Opportunity { get; set; } = null!;
    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}

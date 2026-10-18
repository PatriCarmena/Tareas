using System.ComponentModel.DataAnnotations;

namespace OrderDesk.Web.Models;

public enum OpportunityStage { Prospect, Contacted, Proposal, Negotiation, Won, Lost }

public sealed class Opportunity
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    [Required, StringLength(160)]
    public string Name { get; set; } = string.Empty;

    public decimal EstimatedAmount { get; set; }
    public int Probability { get; set; }
    public DateOnly ExpectedCloseDate { get; set; }
    public OpportunityStage Stage { get; set; } = OpportunityStage.Prospect;

    public string? AssignedUserId { get; set; }
    public AppUser? AssignedUser { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public Order? ConvertedOrder { get; set; }
}

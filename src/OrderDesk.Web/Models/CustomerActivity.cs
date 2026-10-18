using System.ComponentModel.DataAnnotations;

namespace OrderDesk.Web.Models;

public enum ActivityType { Call, Email, Meeting, Task }
public enum ActivityPriority { Low, Normal, High }
public enum ActivityStatus { Pending, Completed }

public sealed class CustomerActivity
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    [Required, StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public ActivityType Type { get; set; }
    public ActivityPriority Priority { get; set; } = ActivityPriority.Normal;
    public ActivityStatus Status { get; set; } = ActivityStatus.Pending;
    public DateTime ScheduledAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }

    public string? AssignedUserId { get; set; }
    public AppUser? AssignedUser { get; set; }
    public string? CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }
}

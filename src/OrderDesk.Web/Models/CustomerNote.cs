using System.ComponentModel.DataAnnotations;

namespace OrderDesk.Web.Models;

public sealed class CustomerNote
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    [Required, StringLength(2000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }
}

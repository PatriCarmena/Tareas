namespace OrderDesk.Web.Models;

public sealed class OrderStatusHistory
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public OrderStatus? PreviousStatus { get; set; }
    public OrderStatus NewStatus { get; set; }
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
    public string? ChangedByUserId { get; set; }
    public AppUser? ChangedByUser { get; set; }
    public string Source { get; set; } = "Aplicación";
}

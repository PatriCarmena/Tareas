using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderDesk.Web.Models;

public enum OrderStatus { Pending, Confirmed, InProgress, Shipped, Delivered, Cancelled }

public sealed class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    [Required, StringLength(80), Display(Name = "Número")]
    public string Number { get; set; } = string.Empty;

    [Required, StringLength(500), Display(Name = "Descripción")]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "numeric(12,2)"), Range(0.01, 999999999), Display(Name = "Total")]
    public decimal Total { get; set; }

    [Display(Name = "Estado")]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<WebhookReceipt> WebhookReceipts { get; set; } = [];
}

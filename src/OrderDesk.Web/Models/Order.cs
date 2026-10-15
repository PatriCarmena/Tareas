using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderDesk.Web.Models;

public enum OrderStatus { Pending, Confirmed, InProgress, Shipped, Delivered, Cancelled }

public sealed class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    [Required(ErrorMessage = "Ingresá el número del pedido."), StringLength(80, ErrorMessage = "El número no puede superar los 80 caracteres."), Display(Name = "Número")]
    public string Number { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá una descripción."), StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres."), Display(Name = "Descripción")]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "numeric(12,2)"), Range(0.01, 999999999, ErrorMessage = "El total debe ser mayor que cero."), Display(Name = "Total")]
    public decimal Total { get; set; }

    [Display(Name = "Estado")]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<WebhookReceipt> WebhookReceipts { get; set; } = [];
    public List<OrderStatusHistory> StatusHistory { get; set; } = [];
}

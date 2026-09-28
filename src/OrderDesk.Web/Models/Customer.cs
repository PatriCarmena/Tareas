using System.ComponentModel.DataAnnotations;

namespace OrderDesk.Web.Models;

public sealed class Customer
{
    public int Id { get; set; }

    [Required, StringLength(120), Display(Name = "Nombre")]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(180)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(40), Display(Name = "Teléfono")]
    public string? Phone { get; set; }

    [StringLength(200), Display(Name = "Dirección")]
    public string? Address { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<Order> Orders { get; set; } = [];
}

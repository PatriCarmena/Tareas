using System.ComponentModel.DataAnnotations;

namespace OrderDesk.Web.Models;

public sealed class Customer
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ingresá el nombre del cliente."), StringLength(120, ErrorMessage = "El nombre no puede superar los 120 caracteres."), Display(Name = "Nombre")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá el email del cliente."), EmailAddress(ErrorMessage = "Ingresá un email válido."), StringLength(180, ErrorMessage = "El email no puede superar los 180 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Ingresá un teléfono válido."), StringLength(40, ErrorMessage = "El teléfono no puede superar los 40 caracteres."), Display(Name = "Teléfono")]
    public string? Phone { get; set; }

    [StringLength(200, ErrorMessage = "La dirección no puede superar los 200 caracteres."), Display(Name = "Dirección")]
    public string? Address { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<Order> Orders { get; set; } = [];
}

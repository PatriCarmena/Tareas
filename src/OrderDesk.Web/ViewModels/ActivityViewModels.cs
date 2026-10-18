using System.ComponentModel.DataAnnotations;
using OrderDesk.Web.Models;

namespace OrderDesk.Web.ViewModels;

public sealed class ActivityFormVm
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccioná un cliente.")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Ingresá un título."), StringLength(160, ErrorMessage = "El título no puede superar los 160 caracteres.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "La descripción no puede superar los 2000 caracteres.")]
    public string? Description { get; set; }

    public ActivityType Type { get; set; }
    public ActivityPriority Priority { get; set; } = ActivityPriority.Normal;

    [Required(ErrorMessage = "Seleccioná una fecha y hora."), Display(Name = "Fecha y hora programada")]
    public DateTime? ScheduledAt { get; set; }

    [Required(ErrorMessage = "Seleccioná un responsable.")]
    public string AssignedUserId { get; set; } = string.Empty;
}

public sealed record ActivityListVm(IReadOnlyList<CustomerActivity> Activities, string? Query, ActivityType? Type,
    string? Status, bool Mine, int Page, int TotalPages, int TotalItems);

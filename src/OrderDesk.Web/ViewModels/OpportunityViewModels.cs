using System.ComponentModel.DataAnnotations;
using OrderDesk.Web.Models;

namespace OrderDesk.Web.ViewModels;

public sealed class OpportunityFormVm
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccioná un cliente.")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Ingresá un nombre."), StringLength(160, ErrorMessage = "El nombre no puede superar los 160 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "9999999999.99", ErrorMessage = "Ingresá un importe entre 0 y 9.999.999.999,99.")]
    public decimal EstimatedAmount { get; set; }

    [Range(0, 100, ErrorMessage = "La probabilidad debe estar entre 0 y 100.")]
    public int Probability { get; set; }

    [Required(ErrorMessage = "Seleccioná una fecha estimada de cierre.")]
    public DateOnly? ExpectedCloseDate { get; set; }

    public OpportunityStage Stage { get; set; } = OpportunityStage.Prospect;

    [StringLength(250, ErrorMessage = "El motivo no puede superar los 250 caracteres.")]
    public string? LostReason { get; set; }

    [Required(ErrorMessage = "Seleccioná un responsable.")]
    public string AssignedUserId { get; set; } = string.Empty;
}

public sealed record OpportunityBoardVm(IReadOnlyList<Opportunity> Opportunities, string? Query,
    string? AssignedUserId, IReadOnlyList<AppUser> Assignees);

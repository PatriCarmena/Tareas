using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OrderDesk.Web.Data;
using OrderDesk.Web.Models;
using OrderDesk.Web.ViewModels;

namespace OrderDesk.Web.Controllers;

[Authorize]
public sealed class OpportunitiesController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? query, string? assignedUserId, CancellationToken ct)
    {
        var items = db.Opportunities.AsNoTracking().Include(x => x.Customer).Include(x => x.AssignedUser).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            items = items.Where(x => EF.Functions.ILike(x.Name, $"%{term}%") || EF.Functions.ILike(x.Customer.Name, $"%{term}%"));
        }
        if (!string.IsNullOrWhiteSpace(assignedUserId)) items = items.Where(x => x.AssignedUserId == assignedUserId);
        var opportunities = await items.OrderBy(x => x.ExpectedCloseDate).ThenBy(x => x.Id).ToListAsync(ct);
        var users = await db.Users.AsNoTracking().OrderBy(x => x.FullName).ToListAsync(ct);
        return View(new OpportunityBoardVm(opportunities, query, assignedUserId, users));
    }

    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Operator)]
    public async Task<IActionResult> Create(int customerId, CancellationToken ct)
    {
        if (!await db.Customers.AnyAsync(x => x.Id == customerId, ct)) return NotFound();
        var model = new OpportunityFormVm
        {
            CustomerId = customerId,
            ExpectedCloseDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(1)),
            AssignedUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty
        };
        await LoadFormOptions(model, ct);
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Operator)]
    public async Task<IActionResult> Create(OpportunityFormVm model, CancellationToken ct)
    {
        await ValidateForm(model, ct);
        if (!ModelState.IsValid) { await LoadFormOptions(model, ct); return View("Form", model); }
        var opportunity = new Opportunity
        {
            CustomerId = model.CustomerId,
            Name = model.Name.Trim(),
            EstimatedAmount = model.EstimatedAmount,
            Probability = model.Probability,
            ExpectedCloseDate = model.ExpectedCloseDate!.Value,
            AssignedUserId = model.AssignedUserId,
            Stage = model.Stage
        };
        db.Opportunities.Add(opportunity);
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Oportunidad creada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Operator)]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var opportunity = await db.Opportunities.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (opportunity is null) return NotFound();
        var model = new OpportunityFormVm
        {
            Id = opportunity.Id,
            CustomerId = opportunity.CustomerId,
            Name = opportunity.Name,
            EstimatedAmount = opportunity.EstimatedAmount,
            Probability = opportunity.Probability,
            ExpectedCloseDate = opportunity.ExpectedCloseDate,
            AssignedUserId = opportunity.AssignedUserId ?? string.Empty,
            Stage = opportunity.Stage
        };
        await LoadFormOptions(model, ct);
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Operator)]
    public async Task<IActionResult> Edit(int id, OpportunityFormVm model, CancellationToken ct)
    {
        if (id != model.Id) return BadRequest();
        var opportunity = await db.Opportunities.FindAsync([id], ct);
        if (opportunity is null) return NotFound();
        if (model.CustomerId != opportunity.CustomerId) return BadRequest();
        await ValidateForm(model, ct);
        if (!ModelState.IsValid) { await LoadFormOptions(model, ct); return View("Form", model); }
        opportunity.Name = model.Name.Trim();
        opportunity.EstimatedAmount = model.EstimatedAmount;
        opportunity.Probability = model.Probability;
        opportunity.ExpectedCloseDate = model.ExpectedCloseDate!.Value;
        opportunity.AssignedUserId = model.AssignedUserId;
        opportunity.Stage = model.Stage;
        opportunity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Oportunidad actualizada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Operator)]
    public async Task<IActionResult> ChangeStage(int id, OpportunityStage stage, CancellationToken ct)
    {
        if (!Enum.IsDefined(stage)) return BadRequest();
        var opportunity = await db.Opportunities.FindAsync([id], ct);
        if (opportunity is null) return NotFound();
        if (opportunity.Stage != stage)
        {
            opportunity.Stage = stage;
            opportunity.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            TempData["Success"] = $"Oportunidad movida a {StageLabel(stage)}.";
        }
        return RedirectToAction(nameof(Index));
    }

    public static string StageLabel(OpportunityStage stage) => stage switch
    {
        OpportunityStage.Prospect => "Prospecto",
        OpportunityStage.Contacted => "Contactado",
        OpportunityStage.Proposal => "Propuesta",
        OpportunityStage.Negotiation => "Negociación",
        OpportunityStage.Won => "Ganada",
        OpportunityStage.Lost => "Perdida",
        _ => stage.ToString()
    };

    private async Task ValidateForm(OpportunityFormVm model, CancellationToken ct)
    {
        if (!Enum.IsDefined(model.Stage)) ModelState.AddModelError(nameof(model.Stage), "Seleccioná una etapa válida.");
        if (!await db.Customers.AnyAsync(x => x.Id == model.CustomerId, ct)) ModelState.AddModelError(nameof(model.CustomerId), "El cliente no existe.");
        if (string.IsNullOrWhiteSpace(model.AssignedUserId) || !await db.Users.AnyAsync(x => x.Id == model.AssignedUserId, ct))
            ModelState.AddModelError(nameof(model.AssignedUserId), "Seleccioná un responsable válido.");
    }

    private async Task LoadFormOptions(OpportunityFormVm model, CancellationToken ct)
    {
        ViewBag.CustomerName = await db.Customers.Where(x => x.Id == model.CustomerId).Select(x => x.Name).FirstOrDefaultAsync(ct);
        var users = await db.Users.AsNoTracking().OrderBy(x => x.FullName).ToListAsync(ct);
        ViewBag.Assignees = new SelectList(users, nameof(AppUser.Id), nameof(AppUser.FullName), model.AssignedUserId);
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OrderDesk.Web.Data;
using OrderDesk.Web.Models;
using OrderDesk.Web.Services;
using OrderDesk.Web.ViewModels;

namespace OrderDesk.Web.Controllers;

[Authorize]
public sealed class ActivitiesController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? query, ActivityType? type, string? status, bool mine = false, int page = 1, CancellationToken ct = default)
    {
        const int pageSize = 12;
        page = Math.Max(1, page);
        var now = DateTime.UtcNow;
        var (_, tomorrowStart) = ActivitySchedule.TodayUtc(now);
        var items = db.CustomerActivities.AsNoTracking().Include(x => x.Customer).Include(x => x.AssignedUser).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            items = items.Where(x => EF.Functions.ILike(x.Title, $"%{term}%") || EF.Functions.ILike(x.Customer.Name, $"%{term}%"));
        }
        if (type.HasValue) items = items.Where(x => x.Type == type.Value);
        if (mine)
        {
            var myId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            items = items.Where(x => x.AssignedUserId == myId);
        }
        items = status switch
        {
            "completed" => items.Where(x => x.Status == ActivityStatus.Completed),
            "overdue" => items.Where(x => x.Status == ActivityStatus.Pending && x.ScheduledAtUtc < now),
            "pending" => items.Where(x => x.Status == ActivityStatus.Pending && x.ScheduledAtUtc >= now),
            "today" => items.Where(x => x.Status == ActivityStatus.Pending && x.ScheduledAtUtc >= now && x.ScheduledAtUtc < tomorrowStart),
            _ => items
        };
        var total = await items.CountAsync(ct);
        var results = await items.OrderBy(x => x.Status).ThenBy(x => x.ScheduledAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return View(new ActivityListVm(results, query, type, status, mine, page, Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)), total));
    }

    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Operator)]
    public async Task<IActionResult> Create(int customerId, CancellationToken ct)
    {
        if (!await db.Customers.AnyAsync(x => x.Id == customerId, ct)) return NotFound();
        var model = new ActivityFormVm
        {
            CustomerId = customerId,
            AssignedUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            ScheduledAt = ActivitySchedule.ToLocal(DateTime.UtcNow).AddHours(1)
        };
        await LoadFormOptions(model, ct);
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Operator)]
    public async Task<IActionResult> Create(ActivityFormVm model, CancellationToken ct)
    {
        await ValidateForm(model, ct);
        if (!ModelState.IsValid) { await LoadFormOptions(model, ct); return View("Form", model); }
        var activity = new CustomerActivity
        {
            CustomerId = model.CustomerId,
            Title = model.Title.Trim(),
            Description = model.Description?.Trim(),
            Type = model.Type,
            Priority = model.Priority,
            ScheduledAtUtc = ActivitySchedule.ToUtc(model.ScheduledAt!.Value),
            AssignedUserId = model.AssignedUserId,
            CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
        };
        db.CustomerActivities.Add(activity);
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Actividad programada correctamente.";
        return RedirectToAction("Details", "Customers", new { id = activity.CustomerId });
    }

    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Operator)]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var activity = await db.CustomerActivities.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (activity is null) return NotFound();
        if (activity.Status == ActivityStatus.Completed) return BadRequest("No se puede editar una actividad realizada.");
        var model = new ActivityFormVm
        {
            Id = activity.Id, CustomerId = activity.CustomerId, Title = activity.Title,
            Description = activity.Description, Type = activity.Type, Priority = activity.Priority,
            ScheduledAt = ActivitySchedule.ToLocal(activity.ScheduledAtUtc), AssignedUserId = activity.AssignedUserId ?? string.Empty
        };
        await LoadFormOptions(model, ct);
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Operator)]
    public async Task<IActionResult> Edit(int id, ActivityFormVm model, CancellationToken ct)
    {
        if (id != model.Id) return BadRequest();
        var activity = await db.CustomerActivities.FindAsync([id], ct);
        if (activity is null) return NotFound();
        if (activity.Status == ActivityStatus.Completed) return BadRequest("No se puede editar una actividad realizada.");
        if (model.CustomerId != activity.CustomerId) return BadRequest();
        await ValidateForm(model, ct);
        if (!ModelState.IsValid) { await LoadFormOptions(model, ct); return View("Form", model); }
        activity.Title = model.Title.Trim();
        activity.Description = model.Description?.Trim();
        activity.Type = model.Type;
        activity.Priority = model.Priority;
        activity.ScheduledAtUtc = ActivitySchedule.ToUtc(model.ScheduledAt!.Value);
        activity.AssignedUserId = model.AssignedUserId;
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Actividad actualizada correctamente.";
        return RedirectToAction("Details", "Customers", new { id = activity.CustomerId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Operator)]
    public async Task<IActionResult> Complete(int id, CancellationToken ct)
    {
        var activity = await db.CustomerActivities.Include(x => x.Customer).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (activity is null) return NotFound();
        if (activity.Status == ActivityStatus.Pending)
        {
            var now = DateTime.UtcNow;
            activity.Status = ActivityStatus.Completed;
            activity.CompletedAtUtc = now;
            if (activity.Type != ActivityType.Task) activity.Customer.LastContactAtUtc = now;
            await db.SaveChangesAsync(ct);
            TempData["Success"] = "Actividad marcada como realizada.";
        }
        return RedirectToAction("Details", "Customers", new { id = activity.CustomerId });
    }

    private async Task ValidateForm(ActivityFormVm model, CancellationToken ct)
    {
        if (!Enum.IsDefined(model.Type)) ModelState.AddModelError(nameof(model.Type), "Seleccioná un tipo válido.");
        if (!Enum.IsDefined(model.Priority)) ModelState.AddModelError(nameof(model.Priority), "Seleccioná una prioridad válida.");
        if (!await db.Customers.AnyAsync(x => x.Id == model.CustomerId, ct)) ModelState.AddModelError(nameof(model.CustomerId), "El cliente no existe.");
        if (string.IsNullOrWhiteSpace(model.AssignedUserId) || !await db.Users.AnyAsync(x => x.Id == model.AssignedUserId, ct))
            ModelState.AddModelError(nameof(model.AssignedUserId), "Seleccioná un responsable válido.");
        if (model.ScheduledAt is not null)
        {
            try { _ = ActivitySchedule.ToUtc(model.ScheduledAt.Value); }
            catch (ArgumentException) { ModelState.AddModelError(nameof(model.ScheduledAt), "Ingresá una fecha y hora válida."); }
        }
    }

    private async Task LoadFormOptions(ActivityFormVm model, CancellationToken ct)
    {
        ViewBag.CustomerName = await db.Customers.Where(x => x.Id == model.CustomerId).Select(x => x.Name).FirstOrDefaultAsync(ct);
        var users = await db.Users.AsNoTracking().OrderBy(x => x.FullName).ToListAsync(ct);
        ViewBag.Assignees = new SelectList(users, nameof(AppUser.Id), nameof(AppUser.FullName), model.AssignedUserId);
    }
}

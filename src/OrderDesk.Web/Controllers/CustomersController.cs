using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderDesk.Web.Data;
using OrderDesk.Web.Models;
using OrderDesk.Web.Services;
using OrderDesk.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace OrderDesk.Web.Controllers;

[Authorize]
public sealed class CustomersController(AppDbContext db, IExternalCustomerService external) : Controller
{
    public async Task<IActionResult> Index(string? query, OrderStatus? status, int page = 1, CancellationToken ct = default)
    {
        const int pageSize = 10;
        page = Math.Max(1, page);
        var customers = db.Customers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            customers = customers.Where(x => EF.Functions.ILike(x.Name, $"%{term}%") || EF.Functions.ILike(x.Email, $"%{term}%") || x.Orders.Any(o => EF.Functions.ILike(o.Number, $"%{term}%")));
        }
        if (status.HasValue) customers = customers.Where(x => x.Orders.Any(o => o.Status == status));
        var total = await customers.CountAsync(ct);
        var items = await customers.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new CustomerListItemVm
        {
            Id = x.Id,
            Name = x.Name,
            Email = x.Email,
            OrderCount = x.Orders.Count(o => !status.HasValue || o.Status == status),
            ActiveTotal = x.Orders.Where(o => (!status.HasValue || o.Status == status) && o.Status != OrderStatus.Cancelled)
                .Sum(o => (decimal?)o.Total) ?? 0,
            LatestOrderStatus = x.Orders.Where(o => !status.HasValue || o.Status == status)
                .OrderByDescending(o => o.CreatedAtUtc).Select(o => (OrderStatus?)o.Status).FirstOrDefault(),
            LatestOrderTotal = x.Orders.Where(o => !status.HasValue || o.Status == status)
                .OrderByDescending(o => o.CreatedAtUtc).Select(o => (decimal?)o.Total).FirstOrDefault()
        }).ToListAsync(ct);
        return View(new CustomerListVm(items, query, status, page, Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)), total));
    }

    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Operator)]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        await LoadAssignees(null, ct);
        return View("Form", new Customer());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Operator)]
    public async Task<IActionResult> Create(Customer customer, CancellationToken ct)
    {
        if (customer.AssignedUserId is not null && !await db.Users.AnyAsync(x => x.Id == customer.AssignedUserId, ct)) ModelState.AddModelError(nameof(customer.AssignedUserId), "El responsable seleccionado no existe.");
        if (!ModelState.IsValid) { await LoadAssignees(customer.AssignedUserId, ct); return View("Form", customer); }
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Cliente creado correctamente.";
        return RedirectToAction(nameof(Details), new { id = customer.Id });
    }

    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Operator)]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var customer = await db.Customers.FindAsync([id], ct);
        if (customer is null) return NotFound();
        await LoadAssignees(customer.AssignedUserId, ct);
        return View("Form", customer);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Operator)]
    public async Task<IActionResult> Edit(int id, Customer input, CancellationToken ct)
    {
        if (id != input.Id) return BadRequest();
        if (input.AssignedUserId is not null && !await db.Users.AnyAsync(x => x.Id == input.AssignedUserId, ct)) ModelState.AddModelError(nameof(input.AssignedUserId), "El responsable seleccionado no existe.");
        if (!ModelState.IsValid) { await LoadAssignees(input.AssignedUserId, ct); return View("Form", input); }
        var customer = await db.Customers.FindAsync([id], ct);
        if (customer is null) return NotFound();
        customer.Name = input.Name; customer.Email = input.Email; customer.Phone = input.Phone; customer.Address = input.Address;
        customer.Company = input.Company; customer.Position = input.Position; customer.AdditionalInfo = input.AdditionalInfo;
        customer.Tags = input.Tags; customer.AssignedUserId = input.AssignedUserId;
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Cambios guardados.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var customer = await db.Customers
            .Include(x => x.AssignedUser)
            .Include(x => x.Notes.OrderByDescending(n => n.CreatedAtUtc)).ThenInclude(n => n.CreatedByUser)
            .Include(x => x.Orders.OrderByDescending(o => o.CreatedAtUtc))
            .ThenInclude(o => o.StatusHistory.OrderByDescending(h => h.ChangedAtUtc)).ThenInclude(h => h.ChangedByUser)
            .AsSplitQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        if (customer is null) return NotFound();
        var activities = await db.CustomerActivities.AsNoTracking()
            .Include(x => x.AssignedUser)
            .Where(x => x.CustomerId == id)
            .OrderBy(x => x.Status).ThenBy(x => x.ScheduledAtUtc)
            .ToListAsync(ct);
        var opportunities = await db.Opportunities.AsNoTracking().Include(x => x.AssignedUser).Include(x => x.ConvertedOrder)
            .Where(x => x.CustomerId == id).OrderBy(x => x.ExpectedCloseDate).ToListAsync(ct);
        var timeline = customer.Notes.Select(n => new CustomerTimelineItemVm(n.CreatedAtUtc, "note", "Nota interna", n.Content, n.CreatedByUser?.FullName))
            .Concat(customer.Orders.Select(o => new CustomerTimelineItemVm(o.CreatedAtUtc, "order", $"Pedido {o.Number}", $"{o.Description} · {o.Total:C0}")))
            .Concat(customer.Orders.SelectMany(o => o.StatusHistory.Select(h => new CustomerTimelineItemVm(h.ChangedAtUtc, "status", $"Estado de {o.Number}", $"{(h.PreviousStatus.HasValue ? OrdersController.StatusLabel(h.PreviousStatus.Value) + " → " : "")} {OrdersController.StatusLabel(h.NewStatus)}", h.ChangedByUser?.FullName ?? h.Source))))
            .Concat(activities.Select(a => new CustomerTimelineItemVm(a.CreatedAtUtc, "activity", $"{ActivitySchedule.TypeLabel(a.Type)} programada: {a.Title}",
                $"Para {ActivitySchedule.ToLocal(a.ScheduledAtUtc):dd/MM/yyyy HH:mm} · Responsable: {a.AssignedUser?.FullName ?? "Sin asignar"}")))
            .Concat(activities.Where(a => a.CompletedAtUtc.HasValue).Select(a => new CustomerTimelineItemVm(a.CompletedAtUtc!.Value, "activity", $"{ActivitySchedule.TypeLabel(a.Type)} realizada: {a.Title}",
                a.Description ?? "Actividad completada")))
            .OrderByDescending(x => x.OccurredAtUtc).ToList();
        return View(new CustomerDetailVm(customer, timeline, activities, opportunities));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Operator)]
    public async Task<IActionResult> AddNote(int id, string content, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Trim().Length > 2000)
        {
            TempData["Error"] = "La nota debe tener entre 1 y 2000 caracteres.";
            return RedirectToAction(nameof(Details), new { id });
        }
        var customer = await db.Customers.FindAsync([id], ct);
        if (customer is null) return NotFound();
        var now = DateTime.UtcNow;
        db.CustomerNotes.Add(new CustomerNote { CustomerId = id, Content = content.Trim(), CreatedAtUtc = now, CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) });
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Nota agregada al historial.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Insight(int id, CancellationToken ct)
    {
        if (!await db.Customers.AsNoTracking().AnyAsync(x => x.Id == id, ct)) return NotFound();
        var insight = await external.GetInsightAsync(id, ct);
        return insight is null
            ? StatusCode(StatusCodes.Status503ServiceUnavailable)
            : Ok(insight);
    }

    private async Task LoadAssignees(string? selected, CancellationToken ct)
    {
        var users = await db.Users.AsNoTracking().OrderBy(x => x.FullName).ToListAsync(ct);
        ViewBag.Assignees = new SelectList(users, nameof(AppUser.Id), nameof(AppUser.FullName), selected);
    }
}

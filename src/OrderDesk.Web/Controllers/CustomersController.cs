using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderDesk.Web.Data;
using OrderDesk.Web.Models;
using OrderDesk.Web.Services;
using OrderDesk.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;

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
    public IActionResult Create() => View("Form", new Customer());

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Operator)]
    public async Task<IActionResult> Create(Customer customer, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View("Form", customer);
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Cliente creado correctamente.";
        return RedirectToAction(nameof(Details), new { id = customer.Id });
    }

    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Operator)]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var customer = await db.Customers.FindAsync([id], ct);
        return customer is null ? NotFound() : View("Form", customer);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Operator)]
    public async Task<IActionResult> Edit(int id, Customer input, CancellationToken ct)
    {
        if (id != input.Id) return BadRequest();
        if (!ModelState.IsValid) return View("Form", input);
        var customer = await db.Customers.FindAsync([id], ct);
        if (customer is null) return NotFound();
        customer.Name = input.Name; customer.Email = input.Email; customer.Phone = input.Phone; customer.Address = input.Address;
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Cambios guardados.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var customer = await db.Customers.Include(x => x.Orders.OrderByDescending(o => o.CreatedAtUtc)).ThenInclude(o => o.StatusHistory.OrderByDescending(h => h.ChangedAtUtc)).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (customer is null) return NotFound();
        var insight = await external.GetInsightAsync(id, ct);
        return View(new CustomerDetailVm(customer, insight));
    }
}

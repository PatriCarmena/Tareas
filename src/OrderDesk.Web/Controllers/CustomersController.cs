using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderDesk.Web.Data;
using OrderDesk.Web.Models;
using OrderDesk.Web.Services;
using OrderDesk.Web.ViewModels;

namespace OrderDesk.Web.Controllers;

public sealed class CustomersController(AppDbContext db, IExternalCustomerService external) : Controller
{
    public async Task<IActionResult> Index(string? query, OrderStatus? status, CancellationToken ct)
    {
        var customers = db.Customers.Include(x => x.Orders).AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            customers = customers.Where(x => EF.Functions.ILike(x.Name, $"%{term}%") || EF.Functions.ILike(x.Email, $"%{term}%") || x.Orders.Any(o => EF.Functions.ILike(o.Number, $"%{term}%")));
        }
        if (status.HasValue) customers = customers.Where(x => x.Orders.Any(o => o.Status == status));
        return View(new CustomerListVm(await customers.OrderBy(x => x.Name).ToListAsync(ct), query, status));
    }

    public IActionResult Create() => View("Form", new Customer());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer customer, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View("Form", customer);
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Cliente creado correctamente.";
        return RedirectToAction(nameof(Details), new { id = customer.Id });
    }

    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var customer = await db.Customers.FindAsync([id], ct);
        return customer is null ? NotFound() : View("Form", customer);
    }

    [HttpPost, ValidateAntiForgeryToken]
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
        var customer = await db.Customers.Include(x => x.Orders.OrderByDescending(o => o.CreatedAtUtc)).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (customer is null) return NotFound();
        var insight = await external.GetInsightAsync(id, ct);
        return View(new CustomerDetailVm(customer, insight));
    }
}

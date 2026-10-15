using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderDesk.Web.Data;
using OrderDesk.Web.Models;
using OrderDesk.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace OrderDesk.Web.Controllers;

[Authorize]
public sealed class OrdersController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? query, OrderStatus? status, int page = 1, CancellationToken ct = default)
    {
        const int pageSize = 12; page = Math.Max(1, page);
        var orders = db.Orders.Include(x => x.Customer).AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query)) { var term = query.Trim(); orders = orders.Where(x => EF.Functions.ILike(x.Number, $"%{term}%") || EF.Functions.ILike(x.Description, $"%{term}%") || EF.Functions.ILike(x.Customer.Name, $"%{term}%")); }
        if (status.HasValue) orders = orders.Where(x => x.Status == status);
        var total = await orders.CountAsync(ct);
        var items = await orders.OrderByDescending(x => x.CreatedAtUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return View(new OrderListVm(items, query, status, page, Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)), total));
    }

    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Operator)]
    public async Task<IActionResult> Create(int customerId, CancellationToken ct)
    {
        var customer = await db.Customers.FindAsync([customerId], ct);
        if (customer is null) return NotFound();
        ViewBag.CustomerName = customer.Name;
        return View(new Order { CustomerId = customerId, Number = $"PED-{DateTime.UtcNow:yyyyMMddHHmm}" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Operator)]
    public async Task<IActionResult> Create(Order order, CancellationToken ct)
    {
        ModelState.Remove(nameof(Order.Customer));
        ModelState.Remove(nameof(Order.WebhookReceipts));
        if (!ModelState.IsValid)
        {
            ViewBag.CustomerName = (await db.Customers.FindAsync([order.CustomerId], ct))?.Name;
            return View(order);
        }
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        db.OrderStatusHistories.Add(new OrderStatusHistory { OrderId = order.Id, NewStatus = order.Status, ChangedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier), Source = "Creación" });
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Pedido creado correctamente.";
        return RedirectToAction("Details", "Customers", new { id = order.CustomerId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Administrator + "," + AppRoles.Operator)]
    public async Task<IActionResult> ChangeStatus(int id, OrderStatus status, string? returnTo, CancellationToken ct)
    {
        var order = await db.Orders.FindAsync([id], ct);
        if (order is null) return NotFound();
        var previous = order.Status;
        if (previous == status)
        {
            TempData["Success"] = $"El pedido {order.Number} ya estaba {StatusLabel(status)}.";
            return RedirectAfterStatusChange(returnTo, order.CustomerId);
        }
        order.Status = status;
        order.UpdatedAtUtc = DateTime.UtcNow;
        db.OrderStatusHistories.Add(new OrderStatusHistory { OrderId = order.Id, PreviousStatus = previous, NewStatus = status, ChangedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) });
        await db.SaveChangesAsync(ct);
        TempData["Success"] = $"El pedido {order.Number} ahora está {StatusLabel(status)}.";
        return RedirectAfterStatusChange(returnTo, order.CustomerId);
    }

    private IActionResult RedirectAfterStatusChange(string? returnTo, int customerId) =>
        string.Equals(returnTo, "orders", StringComparison.OrdinalIgnoreCase)
            ? RedirectToAction(nameof(Index))
            : RedirectToAction("Details", "Customers", new { id = customerId });

    public static string StatusLabel(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Pendiente", OrderStatus.Confirmed => "Confirmado", OrderStatus.InProgress => "En preparación",
        OrderStatus.Shipped => "Enviado", OrderStatus.Delivered => "Entregado", OrderStatus.Cancelled => "Cancelado", _ => status.ToString()
    };
}

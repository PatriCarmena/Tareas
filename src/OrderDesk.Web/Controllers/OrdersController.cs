using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderDesk.Web.Data;
using OrderDesk.Web.Models;

namespace OrderDesk.Web.Controllers;

public sealed class OrdersController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Create(int customerId, CancellationToken ct)
    {
        var customer = await db.Customers.FindAsync([customerId], ct);
        if (customer is null) return NotFound();
        ViewBag.CustomerName = customer.Name;
        return View(new Order { CustomerId = customerId, Number = $"PED-{DateTime.UtcNow:yyyyMMddHHmm}" });
    }

    [HttpPost, ValidateAntiForgeryToken]
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
        TempData["Success"] = "Pedido creado correctamente.";
        return RedirectToAction("Details", "Customers", new { id = order.CustomerId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, OrderStatus status, CancellationToken ct)
    {
        var order = await db.Orders.FindAsync([id], ct);
        if (order is null) return NotFound();
        order.Status = status;
        order.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        TempData["Success"] = $"El pedido {order.Number} ahora está {StatusLabel(status)}.";
        return RedirectToAction("Details", "Customers", new { id = order.CustomerId });
    }

    public static string StatusLabel(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Pendiente", OrderStatus.Confirmed => "Confirmado", OrderStatus.InProgress => "En preparación",
        OrderStatus.Shipped => "Enviado", OrderStatus.Delivered => "Entregado", OrderStatus.Cancelled => "Cancelado", _ => status.ToString()
    };
}

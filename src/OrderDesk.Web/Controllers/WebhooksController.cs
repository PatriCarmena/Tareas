using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderDesk.Web.Data;
using OrderDesk.Web.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace OrderDesk.Web.Controllers;

[ApiController, AllowAnonymous, Route("api/webhooks/orders")]
public sealed class WebhooksController(AppDbContext db, IConfiguration configuration) : ControllerBase
{
    public sealed record OrderStatusEvent(string EventId, int OrderId, OrderStatus Status);

    [HttpPost]
    public async Task<IActionResult> UpdateOrder([FromBody] OrderStatusEvent message, CancellationToken ct)
    {
        var expected = configuration["Webhook:Secret"] ?? string.Empty;
        var supplied = Request.Headers["X-Webhook-Secret"].ToString();
        if (string.IsNullOrEmpty(expected) || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(supplied)))
            return Unauthorized();
        if (string.IsNullOrWhiteSpace(message.EventId) || message.EventId.Length > 160)
            return BadRequest(new { error = "eventId debe tener entre 1 y 160 caracteres" });
        if (!Enum.IsDefined(message.Status)) return BadRequest(new { error = "Estado inválido" });
        if (await db.WebhookReceipts.AnyAsync(x => x.EventId == message.EventId, ct)) return Ok(new { duplicate = true });
        var order = await db.Orders.FindAsync([message.OrderId], ct);
        if (order is null) return NotFound(new { error = "Pedido inexistente" });
        var previous = order.Status;
        order.Status = message.Status;
        order.UpdatedAtUtc = DateTime.UtcNow;
        if (previous != message.Status) db.OrderStatusHistories.Add(new OrderStatusHistory { OrderId = order.Id, PreviousStatus = previous, NewStatus = message.Status, Source = "Webhook" });
        db.WebhookReceipts.Add(new WebhookReceipt { EventId = message.EventId, OrderId = order.Id });
        await db.SaveChangesAsync(ct);
        return Ok(new { updated = true, order.Id, status = order.Status.ToString() });
    }
}

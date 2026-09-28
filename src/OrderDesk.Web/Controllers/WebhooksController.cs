using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderDesk.Web.Data;
using OrderDesk.Web.Models;
using System.Security.Cryptography;
using System.Text;

namespace OrderDesk.Web.Controllers;

[ApiController, Route("api/webhooks/orders")]
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
        if (string.IsNullOrWhiteSpace(message.EventId)) return BadRequest(new { error = "eventId es obligatorio" });
        if (await db.WebhookReceipts.AnyAsync(x => x.EventId == message.EventId, ct)) return Ok(new { duplicate = true });
        var order = await db.Orders.FindAsync([message.OrderId], ct);
        if (order is null) return NotFound(new { error = "Pedido inexistente" });
        order.Status = message.Status;
        order.UpdatedAtUtc = DateTime.UtcNow;
        db.WebhookReceipts.Add(new WebhookReceipt { EventId = message.EventId, OrderId = order.Id });
        await db.SaveChangesAsync(ct);
        return Ok(new { updated = true, order.Id, status = order.Status.ToString() });
    }
}

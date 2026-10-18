using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderDesk.Web.Data;
using OrderDesk.Web.Models;
using OrderDesk.Web.Services;

namespace OrderDesk.Web.Controllers;

[ApiController, AllowAnonymous, Route("api/webhooks/opportunities")]
public sealed class OpportunityWebhooksController(AppDbContext db, IConfiguration configuration) : ControllerBase
{
    public sealed record OpportunityStageEvent(string EventId, int OpportunityId, OpportunityStage Stage);

    [HttpPost]
    public async Task<IActionResult> UpdateStage([FromBody] OpportunityStageEvent message, CancellationToken ct)
    {
        var expected = configuration["Webhook:Secret"] ?? string.Empty;
        var supplied = Request.Headers["X-Webhook-Secret"].ToString();
        if (string.IsNullOrEmpty(expected) || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(supplied)))
            return Unauthorized();
        if (string.IsNullOrWhiteSpace(message.EventId) || message.EventId.Length > 160)
            return BadRequest(new { error = "eventId debe tener entre 1 y 160 caracteres" });
        if (!Enum.IsDefined(message.Stage)) return BadRequest(new { error = "Etapa inválida" });
        if (await db.OpportunityWebhookReceipts.AnyAsync(x => x.EventId == message.EventId, ct))
            return Ok(new { duplicate = true });

        var opportunity = await db.Opportunities.SingleOrDefaultAsync(x => x.Id == message.OpportunityId, ct);
        if (opportunity is null) return NotFound(new { error = "Oportunidad inexistente" });
        if (message.Stage != OpportunityStage.Won && await db.Orders.AnyAsync(x => x.OpportunityId == opportunity.Id, ct))
            return Conflict(new { error = "La oportunidad ya se convirtió en pedido y debe permanecer ganada" });
        if (opportunity.Stage != message.Stage)
        {
            opportunity.Stage = message.Stage;
            opportunity.UpdatedAtUtc = DateTime.UtcNow;
            await OpportunityAutomation.QueueFollowUpAsync(db, opportunity, ct);
        }
        db.OpportunityWebhookReceipts.Add(new OpportunityWebhookReceipt { EventId = message.EventId, OpportunityId = opportunity.Id });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Ok(new { duplicate = true });
        }
        return Ok(new { updated = true, opportunity.Id, stage = opportunity.Stage.ToString() });
    }
}

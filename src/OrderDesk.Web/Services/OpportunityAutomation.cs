using Microsoft.EntityFrameworkCore;
using OrderDesk.Web.Data;
using OrderDesk.Web.Models;

namespace OrderDesk.Web.Services;

public static class OpportunityAutomation
{
    public static async Task QueueFollowUpAsync(AppDbContext db, Opportunity opportunity, CancellationToken ct)
    {
        if (opportunity.Stage is not (OpportunityStage.Proposal or OpportunityStage.Negotiation)) return;
        var title = opportunity.Stage == OpportunityStage.Proposal
            ? $"Seguimiento de propuesta: {opportunity.Name}"
            : $"Seguimiento de negociación: {opportunity.Name}";
        title = title[..Math.Min(title.Length, 160)];
        var alreadyPending = await db.CustomerActivities.AnyAsync(x => x.CustomerId == opportunity.CustomerId
            && x.Title == title && x.Status == ActivityStatus.Pending, ct);
        if (alreadyPending) return;
        db.CustomerActivities.Add(new CustomerActivity
        {
            CustomerId = opportunity.CustomerId,
            Title = title,
            Description = $"Generada automáticamente al mover la oportunidad #{opportunity.Id} a {OpportunitiesLabel(opportunity.Stage)}.",
            Type = ActivityType.Task,
            Priority = ActivityPriority.Normal,
            ScheduledAtUtc = DateTime.UtcNow.AddDays(2),
            AssignedUserId = opportunity.AssignedUserId
        });
    }

    public static void QueuePostSaleTask(AppDbContext db, Opportunity opportunity)
    {
        var title = $"Seguimiento posventa: {opportunity.Name}";
        db.CustomerActivities.Add(new CustomerActivity
        {
            CustomerId = opportunity.CustomerId,
            Title = title[..Math.Min(title.Length, 160)],
            Description = $"Generada automáticamente al convertir la oportunidad #{opportunity.Id} en pedido.",
            Type = ActivityType.Task,
            Priority = ActivityPriority.Normal,
            ScheduledAtUtc = DateTime.UtcNow.AddDays(3),
            AssignedUserId = opportunity.AssignedUserId
        });
    }

    private static string OpportunitiesLabel(OpportunityStage stage) => stage == OpportunityStage.Proposal ? "Propuesta" : "Negociación";
}

using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderDesk.Web.Data;
using OrderDesk.Web.Models;
using OrderDesk.Web.Services;
using OrderDesk.Web.ViewModels;

namespace OrderDesk.Web.Controllers;

[Authorize(Roles = AppRoles.Administrator)]
public sealed class ReportsController(AppDbContext db) : Controller
{
    private static readonly CultureInfo Argentina = CultureInfo.GetCultureInfo("es-AR");

    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, string? salespersonId, CancellationToken ct)
    {
        var (start, end) = ResolvePeriod(from, to);
        if (start > end)
        {
            ModelState.AddModelError(string.Empty, "La fecha inicial no puede ser posterior a la final.");
            (start, end) = ResolvePeriod(null, null);
        }
        return View(await BuildReportAsync(start, end, salespersonId, ct));
    }

    public async Task<IActionResult> Export(string kind, DateOnly? from, DateOnly? to, string? salespersonId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(kind)) return NotFound();
        var (start, end) = ResolvePeriod(from, to);
        if (start > end) return BadRequest("La fecha inicial no puede ser posterior a la final.");
        var report = await BuildReportAsync(start, end, salespersonId, ct);
        var rows = new List<string?[]>();
        switch (kind.ToLowerInvariant())
        {
            case "sales":
                rows.Add(["Fecha", "Pedido", "Cliente", "Vendedor", "Importe", "Estado"]);
                rows.AddRange(report.SalesOrders.Select(x => new string?[]
                {
                    ActivitySchedule.ToLocal(x.CreatedAtUtc).ToString("dd/MM/yyyy"), x.Number, x.Customer.Name,
                    x.SalespersonName ?? "Sin vendedor", x.Total.ToString("0.00", Argentina), OrdersController.StatusLabel(x.Status)
                }));
                break;
            case "opportunities":
                rows.Add(["Creada", "Cliente", "Oportunidad", "Etapa", "Importe estimado", "Probabilidad %", "Cierre previsto", "Responsable", "Fecha de cierre", "Motivo de pérdida"]);
                rows.AddRange(report.Opportunities.Select(x => new string?[]
                {
                    ActivitySchedule.ToLocal(x.CreatedAtUtc).ToString("dd/MM/yyyy"), x.Customer.Name, x.Name,
                    OpportunitiesController.StageLabel(x.Stage), x.EstimatedAmount.ToString("0.00", Argentina), x.Probability.ToString(Argentina),
                    x.ExpectedCloseDate.ToString("dd/MM/yyyy"), x.AssignedUser?.FullName ?? "Sin responsable",
                    x.ClosedAtUtc.HasValue ? ActivitySchedule.ToLocal(x.ClosedAtUtc.Value).ToString("dd/MM/yyyy") : "",
                    x.LostReason
                }));
                break;
            case "activities":
                rows.Add(["Cliente", "Actividad", "Tipo", "Programada", "Responsable", "Prioridad", "Estado"]);
                rows.AddRange(report.OpenActivities.Select(x => new string?[]
                {
                    x.Customer.Name, x.Title, ActivitySchedule.TypeLabel(x.Type), ActivitySchedule.ToLocal(x.ScheduledAtUtc).ToString("dd/MM/yyyy HH:mm"),
                    x.AssignedUser?.FullName ?? "Sin responsable", ActivitySchedule.PriorityLabel(x.Priority), ActivitySchedule.EffectiveStatus(x, DateTime.UtcNow)
                }));
                break;
            case "customers":
                rows.Add(["Cliente", "Email", "Responsable", "Último contacto", "Creado"]);
                rows.AddRange(report.InactiveCustomers.Select(x => new string?[]
                {
                    x.Name, x.Email, x.AssignedUser?.FullName ?? "Sin responsable",
                    x.LastContactAtUtc.HasValue ? ActivitySchedule.ToLocal(x.LastContactAtUtc.Value).ToString("dd/MM/yyyy") : "Nunca",
                    ActivitySchedule.ToLocal(x.CreatedAtUtc).ToString("dd/MM/yyyy")
                }));
                break;
            default:
                return NotFound();
        }
        return File(CsvExport.Build(rows), "text/csv; charset=utf-8", $"nexo-{kind}-{start:yyyyMMdd}-{end:yyyyMMdd}.csv");
    }

    private static (DateOnly Start, DateOnly End) ResolvePeriod(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(ActivitySchedule.ToLocal(DateTime.UtcNow));
        return (from ?? today.AddDays(-89), to ?? today);
    }

    private async Task<ReportVm> BuildReportAsync(DateOnly from, DateOnly to, string? salespersonId, CancellationToken ct)
    {
        var startUtc = ActivitySchedule.ToUtc(from.ToDateTime(TimeOnly.MinValue));
        var endUtc = ActivitySchedule.ToUtc(to.AddDays(1).ToDateTime(TimeOnly.MinValue));
        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(-30);
        var salesQuery = db.Orders.AsNoTracking().Include(x => x.Customer)
            .Where(x => x.Status != OrderStatus.Cancelled && x.CreatedAtUtc >= startUtc && x.CreatedAtUtc < endUtc);
        var opportunitiesQuery = db.Opportunities.AsNoTracking().Include(x => x.Customer).Include(x => x.AssignedUser)
            .Where(x => x.CreatedAtUtc >= startUtc && x.CreatedAtUtc < endUtc);
        var activitiesQuery = db.CustomerActivities.AsNoTracking().Include(x => x.Customer).Include(x => x.AssignedUser)
            .Where(x => x.Status == ActivityStatus.Pending);
        var customersQuery = db.Customers.AsNoTracking().Include(x => x.AssignedUser)
            .Where(x => (x.LastContactAtUtc ?? x.CreatedAtUtc) < cutoff);
        if (!string.IsNullOrWhiteSpace(salespersonId))
        {
            salesQuery = salesQuery.Where(x => x.SalespersonId == salespersonId);
            opportunitiesQuery = opportunitiesQuery.Where(x => x.AssignedUserId == salespersonId);
            activitiesQuery = activitiesQuery.Where(x => x.AssignedUserId == salespersonId);
            customersQuery = customersQuery.Where(x => x.AssignedUserId == salespersonId);
        }

        var sales = await salesQuery.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
        var opportunities = await opportunitiesQuery.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
        var activities = await activitiesQuery.OrderBy(x => x.ScheduledAtUtc).ToListAsync(ct);
        var customers = await customersQuery.OrderBy(x => x.LastContactAtUtc ?? x.CreatedAtUtc).ToListAsync(ct);
        var sellers = await db.Users.AsNoTracking().OrderBy(x => x.FullName).ToListAsync(ct);
        var won = opportunities.Where(x => x.Stage == OpportunityStage.Won).ToList();
        var lost = opportunities.Where(x => x.Stage == OpportunityStage.Lost).ToList();
        var closingDays = won.Where(x => x.ClosedAtUtc.HasValue)
            .Select(x => Math.Max(0, (x.ClosedAtUtc!.Value - x.CreatedAtUtc).TotalDays)).ToList();

        return new ReportVm
        {
            From = from, To = to, SalespersonId = salespersonId, Sellers = sellers,
            SalesOrders = sales, Opportunities = opportunities, OpenActivities = activities, InactiveCustomers = customers,
            SalesTotal = sales.Sum(x => x.Total), WonCount = won.Count, LostCount = lost.Count,
            ConversionRate = won.Count + lost.Count == 0 ? 0 : Math.Round(100.0 * won.Count / (won.Count + lost.Count), 1),
            AverageCloseDays = closingDays.Count == 0 ? null : Math.Round(closingDays.Average(), 1),
            CloseSampleCount = closingDays.Count,
            PendingCount = activities.Count(x => x.ScheduledAtUtc >= now),
            OverdueCount = activities.Count(x => x.ScheduledAtUtc < now),
            SalesByPeriod = sales.GroupBy(x => DateOnly.FromDateTime(ActivitySchedule.ToLocal(x.CreatedAtUtc)))
                .Select(g => new SalesPeriodVm(g.Key, g.Count(), g.Sum(x => x.Total))).OrderBy(x => x.Day).ToList(),
            SalesBySeller = sales.GroupBy(x => x.SalespersonName ?? "Sin vendedor")
                .Select(g => new SalesSellerVm(g.Key, g.Count(), g.Sum(x => x.Total))).OrderByDescending(x => x.Total).ToList(),
            ByStage = Enum.GetValues<OpportunityStage>().Select(stage => new OpportunityStageVm(stage,
                opportunities.Count(x => x.Stage == stage), opportunities.Where(x => x.Stage == stage).Sum(x => x.EstimatedAmount))).ToList(),
            LostReasons = lost.GroupBy(x => string.IsNullOrWhiteSpace(x.LostReason) ? "Sin registrar" : x.LostReason.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new LostReasonVm(g.Key, g.Count())).OrderByDescending(x => x.Count).ToList()
        };
    }
}

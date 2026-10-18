using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderDesk.Web.Data;
using OrderDesk.Web.Models;
using OrderDesk.Web.ViewModels;
using OrderDesk.Web.Services;
using System.Security.Claims;

namespace OrderDesk.Web.Controllers;

[Authorize]
public sealed class DashboardController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var active = db.Orders.AsNoTracking().Where(x => x.Status != OrderStatus.Cancelled);
        var data = await active.GroupBy(x => x.Status).Select(x => new { Status = x.Key, Count = x.Count(), Total = x.Sum(o => o.Total) }).ToListAsync(ct);
        var grouped = data.OrderBy(x => x.Status).Select(x => new DashboardStatusVm(x.Status, x.Count, x.Total)).ToList();
        var now = DateTime.UtcNow;
        var (todayStart, tomorrowStart) = ActivitySchedule.TodayUtc(now);
        var openActivities = db.CustomerActivities.AsNoTracking().Where(x => x.Status == ActivityStatus.Pending);
        var dueToday = await openActivities.CountAsync(x => x.ScheduledAtUtc >= now && x.ScheduledAtUtc < tomorrowStart, ct);
        var overdue = await openActivities.CountAsync(x => x.ScheduledAtUtc < now, ct);
        var myId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var myTasks = await openActivities.Include(x => x.Customer)
            .Where(x => x.AssignedUserId == myId && x.ScheduledAtUtc >= todayStart && x.ScheduledAtUtc < tomorrowStart)
            .OrderBy(x => x.ScheduledAtUtc).Take(10).ToListAsync(ct);
        return View(new DashboardVm(await db.Customers.CountAsync(ct), await active.CountAsync(ct), await active.SumAsync(x => (decimal?)x.Total, ct) ?? 0,
            grouped, dueToday, overdue, myTasks));
    }
}

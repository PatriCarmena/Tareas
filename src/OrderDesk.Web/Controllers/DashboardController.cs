using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderDesk.Web.Data;
using OrderDesk.Web.Models;
using OrderDesk.Web.ViewModels;

namespace OrderDesk.Web.Controllers;

[Authorize]
public sealed class DashboardController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var active = db.Orders.AsNoTracking().Where(x => x.Status != OrderStatus.Cancelled);
        var data = await active.GroupBy(x => x.Status).Select(x => new { Status = x.Key, Count = x.Count(), Total = x.Sum(o => o.Total) }).ToListAsync(ct);
        var grouped = data.OrderBy(x => x.Status).Select(x => new DashboardStatusVm(x.Status, x.Count, x.Total)).ToList();
        return View(new DashboardVm(await db.Customers.CountAsync(ct), await active.CountAsync(ct), await active.SumAsync(x => (decimal?)x.Total, ct) ?? 0, grouped));
    }
}

using OrderDesk.Web.Models;

namespace OrderDesk.Web.ViewModels;

public sealed record SalesPeriodVm(DateOnly Day, int Count, decimal Total);
public sealed record SalesSellerVm(string Seller, int Count, decimal Total);
public sealed record OpportunityStageVm(OpportunityStage Stage, int Count, decimal Total);
public sealed record LostReasonVm(string Reason, int Count);

public sealed class ReportVm
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public string? SalespersonId { get; init; }
    public IReadOnlyList<AppUser> Sellers { get; init; } = [];
    public IReadOnlyList<Order> SalesOrders { get; init; } = [];
    public IReadOnlyList<Opportunity> Opportunities { get; init; } = [];
    public IReadOnlyList<CustomerActivity> OpenActivities { get; init; } = [];
    public IReadOnlyList<Customer> InactiveCustomers { get; init; } = [];
    public IReadOnlyList<SalesPeriodVm> SalesByPeriod { get; init; } = [];
    public IReadOnlyList<SalesSellerVm> SalesBySeller { get; init; } = [];
    public IReadOnlyList<OpportunityStageVm> ByStage { get; init; } = [];
    public IReadOnlyList<LostReasonVm> LostReasons { get; init; } = [];
    public decimal SalesTotal { get; init; }
    public int WonCount { get; init; }
    public int LostCount { get; init; }
    public double ConversionRate { get; init; }
    public double? AverageCloseDays { get; init; }
    public int CloseSampleCount { get; init; }
    public int PendingCount { get; init; }
    public int OverdueCount { get; init; }
}

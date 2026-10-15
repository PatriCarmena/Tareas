using OrderDesk.Web.Models;
using OrderDesk.Web.Services;

namespace OrderDesk.Web.ViewModels;

public sealed class CustomerListItemVm
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public int OrderCount { get; init; }
    public decimal ActiveTotal { get; init; }
    public OrderStatus? LatestOrderStatus { get; init; }
    public decimal? LatestOrderTotal { get; init; }
}

public sealed record CustomerListVm(IReadOnlyList<CustomerListItemVm> Customers, string? Query, OrderStatus? Status, int Page, int TotalPages, int TotalItems);
public sealed record CustomerTimelineItemVm(DateTime OccurredAtUtc, string Type, string Title, string Description, string? Author = null);
public sealed record CustomerDetailVm(Customer Customer, IReadOnlyList<CustomerTimelineItemVm> Timeline);
public sealed record OrderListVm(IReadOnlyList<Order> Orders, string? Query, OrderStatus? Status, int Page, int TotalPages, int TotalItems);
public sealed record DashboardStatusVm(OrderStatus Status, int Count, decimal Total);
public sealed record DashboardVm(int CustomerCount, int OrderCount, decimal Total, IReadOnlyList<DashboardStatusVm> ByStatus);

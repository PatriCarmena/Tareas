using OrderDesk.Web.Models;
using OrderDesk.Web.Services;

namespace OrderDesk.Web.ViewModels;

public sealed record CustomerListVm(IReadOnlyList<Customer> Customers, string? Query, OrderStatus? Status, int Page, int TotalPages, int TotalItems);
public sealed record CustomerDetailVm(Customer Customer, CustomerInsight? Insight);
public sealed record OrderListVm(IReadOnlyList<Order> Orders, string? Query, OrderStatus? Status, int Page, int TotalPages, int TotalItems);
public sealed record DashboardStatusVm(OrderStatus Status, int Count, decimal Total);
public sealed record DashboardVm(int CustomerCount, int OrderCount, decimal Total, IReadOnlyList<DashboardStatusVm> ByStatus);

using OrderDesk.Web.Models;
using OrderDesk.Web.Services;

namespace OrderDesk.Web.ViewModels;

public sealed record CustomerListVm(IReadOnlyList<Customer> Customers, string? Query, OrderStatus? Status);
public sealed record CustomerDetailVm(Customer Customer, CustomerInsight? Insight);

using OrderDesk.Web.Models;
namespace OrderDesk.Web.ViewModels;
public sealed record PaginationVm(int Page, int TotalPages, string? Query, OrderStatus? Status);

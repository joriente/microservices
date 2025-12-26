using ErrorOr;
using ProductOrderingSystem.OrderService.Domain.Entities;

namespace ProductOrderingSystem.OrderService.Application.Queries.Orders;

public record GetOrdersQuery(
    string? CustomerId = null,
    OrderStatus? Status = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int Page = 1,
    int PageSize = 10
);

public record GetOrdersResult(
    List<Order> Orders,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

using ErrorOr;
using MediatR;
using ProductOrderingSystem.OrderService.Domain.Entities;

namespace ProductOrderingSystem.OrderService.Application.Queries.Orders;

public record GetOrdersQuery(
    string? CustomerId = null,
    OrderStatus? Status = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int Page = 1,
    int PageSize = 10
) : IRequest<ErrorOr<GetOrdersResult>>;

public record GetOrdersResult(
    List<Order> Orders,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

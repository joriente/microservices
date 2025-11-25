using ErrorOr;
using MediatR;
using ProductOrderingSystem.OrderService.Domain.Entities;

namespace ProductOrderingSystem.OrderService.Application.Queries.Orders;

public record GetOrderByIdQuery(string OrderId) : IRequest<ErrorOr<Order>>;

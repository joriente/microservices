using ErrorOr;
using MediatR;
using ProductOrderingSystem.OrderService.Domain.Entities;

namespace ProductOrderingSystem.OrderService.Application.Commands.Orders;

public record CreateOrderCommand(
    string CustomerId,
    string CustomerEmail,
    string CustomerName,
    List<CreateOrderItemCommand> Items,
    string? Notes = null
) : IRequest<ErrorOr<Order>>;

public record CreateOrderItemCommand(
    string ProductId,
    string ProductName,
    decimal Price,
    int Quantity
);
// Note: ProductName and Price required from client until event-driven sync is working
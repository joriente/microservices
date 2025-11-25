using ErrorOr;
using MediatR;

namespace ProductOrderingSystem.CartService.Application.Queries.Carts;

public record GetCartByIdQuery(string CartId) : IRequest<ErrorOr<CartDto>>;

public record CartDto(
    string Id,
    string CustomerId,
    string CustomerEmail,
    List<CartItemDto> Items,
    decimal TotalAmount,
    int TotalItems,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CartItemDto(
    string ProductId,
    string ProductName,
    decimal Price,
    int Quantity,
    decimal TotalPrice
);

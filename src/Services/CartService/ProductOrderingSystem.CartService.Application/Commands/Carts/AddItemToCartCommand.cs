using ErrorOr;

namespace ProductOrderingSystem.CartService.Application.Commands.Carts;

public record AddItemToCartCommand(
    string CartId,
    string ProductId,
    string ProductName,
    decimal Price,
    int Quantity
);

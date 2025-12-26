using ErrorOr;

namespace ProductOrderingSystem.CartService.Application.Commands.Carts;

public record UpdateItemQuantityCommand(
    string CartId,
    string ProductId,
    int Quantity
);

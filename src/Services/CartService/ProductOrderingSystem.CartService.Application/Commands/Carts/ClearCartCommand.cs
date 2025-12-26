using ErrorOr;

namespace ProductOrderingSystem.CartService.Application.Commands.Carts;

public record ClearCartCommand(
    string CartId
);

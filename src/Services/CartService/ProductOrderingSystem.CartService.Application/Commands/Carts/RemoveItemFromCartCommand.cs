using ErrorOr;

namespace ProductOrderingSystem.CartService.Application.Commands.Carts;

public record RemoveItemFromCartCommand(
    string CartId,
    string ProductId
);

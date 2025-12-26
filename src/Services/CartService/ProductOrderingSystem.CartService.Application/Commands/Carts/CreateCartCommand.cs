using ErrorOr;

namespace ProductOrderingSystem.CartService.Application.Commands.Carts;

public record CreateCartCommand(
    string CustomerId,
    string CustomerEmail
);

public record CreateCartResponse(
    string CartId,
    string CustomerId,
    string CustomerEmail,
    DateTime CreatedAt
);

using ErrorOr;
using MediatR;

namespace ProductOrderingSystem.CartService.Application.Commands.Carts;

public record CreateCartCommand(
    string CustomerId,
    string CustomerEmail
) : IRequest<ErrorOr<CreateCartResponse>>;

public record CreateCartResponse(
    string CartId,
    string CustomerId,
    string CustomerEmail,
    DateTime CreatedAt
);

using ErrorOr;
using MediatR;

namespace ProductOrderingSystem.CartService.Application.Commands.Carts;

public record UpdateItemQuantityCommand(
    string CartId,
    string ProductId,
    int Quantity
) : IRequest<ErrorOr<Success>>;

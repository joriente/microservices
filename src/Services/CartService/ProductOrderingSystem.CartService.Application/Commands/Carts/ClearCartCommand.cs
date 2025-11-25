using ErrorOr;
using MediatR;

namespace ProductOrderingSystem.CartService.Application.Commands.Carts;

public record ClearCartCommand(
    string CartId
) : IRequest<ErrorOr<Success>>;

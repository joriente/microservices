using ErrorOr;
using MediatR;

namespace ProductOrderingSystem.CartService.Application.Commands.Carts;

public record RemoveItemFromCartCommand(
    string CartId,
    string ProductId
) : IRequest<ErrorOr<Success>>;

using ErrorOr;
using MediatR;
using ProductOrderingSystem.CartService.Domain.Repositories;

namespace ProductOrderingSystem.CartService.Application.Commands.Carts;

public class ClearCartCommandHandler : IRequestHandler<ClearCartCommand, ErrorOr<Success>>
{
    private readonly ICartRepository _cartRepository;

    public ClearCartCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<ErrorOr<Success>> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByIdAsync(request.CartId);
        if (cart == null)
        {
            return Error.NotFound("Cart.NotFound", $"Cart with ID {request.CartId} not found");
        }

        cart.Clear();
        await _cartRepository.UpdateAsync(cart);
        return Result.Success;
    }
}

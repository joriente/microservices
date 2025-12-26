using ErrorOr;
using ProductOrderingSystem.CartService.Domain.Repositories;

namespace ProductOrderingSystem.CartService.Application.Commands.Carts;

public class UpdateItemQuantityCommandHandler
{
    private readonly ICartRepository _cartRepository;

    public UpdateItemQuantityCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<ErrorOr<Success>> Handle(UpdateItemQuantityCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByIdAsync(request.CartId);
        if (cart == null)
        {
            return Error.NotFound("Cart.NotFound", $"Cart with ID {request.CartId} not found");
        }

        try
        {
            cart.UpdateItemQuantity(request.ProductId, request.Quantity);
            await _cartRepository.UpdateAsync(cart);
            return Result.Success;
        }
        catch (InvalidOperationException ex)
        {
            return Error.NotFound("CartItem.NotFound", ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation("Cart.InvalidQuantity", ex.Message);
        }
    }
}

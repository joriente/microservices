using ErrorOr;
using ProductOrderingSystem.CartService.Domain.Repositories;

namespace ProductOrderingSystem.CartService.Application.Commands.Carts;

public class AddItemToCartCommandHandler
{
    private readonly ICartRepository _cartRepository;

    public AddItemToCartCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<ErrorOr<Success>> Handle(AddItemToCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByIdAsync(request.CartId);
        if (cart == null)
        {
            return Error.NotFound("Cart.NotFound", $"Cart with ID {request.CartId} not found");
        }

        try
        {
            cart.AddItem(request.ProductId, request.ProductName, request.Price, request.Quantity);
            await _cartRepository.UpdateAsync(cart);
            return Result.Success;
        }
        catch (ArgumentException ex)
        {
            return Error.Validation("Cart.InvalidItem", ex.Message);
        }
    }
}

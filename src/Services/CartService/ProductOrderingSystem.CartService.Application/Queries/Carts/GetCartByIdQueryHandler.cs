using ErrorOr;
using ProductOrderingSystem.CartService.Domain.Repositories;

namespace ProductOrderingSystem.CartService.Application.Queries.Carts;

public class GetCartByIdQueryHandler
{
    private readonly ICartRepository _cartRepository;

    public GetCartByIdQueryHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<ErrorOr<CartDto>> Handle(GetCartByIdQuery request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByIdAsync(request.CartId);
        if (cart == null)
        {
            return Error.NotFound("Cart.NotFound", $"Cart with ID {request.CartId} not found");
        }

        var cartDto = new CartDto(
            cart.Id,
            cart.CustomerId,
            cart.CustomerEmail,
            cart.Items.Select(item => new CartItemDto(
                item.ProductId,
                item.ProductName,
                item.Price,
                item.Quantity,
                item.TotalPrice
            )).ToList(),
            cart.TotalAmount,
            cart.TotalItems,
            cart.CreatedAt,
            cart.UpdatedAt
        );

        return cartDto;
    }
}

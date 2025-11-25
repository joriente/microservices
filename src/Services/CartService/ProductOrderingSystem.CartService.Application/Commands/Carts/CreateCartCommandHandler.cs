using ErrorOr;
using MediatR;
using ProductOrderingSystem.CartService.Domain.Entities;
using ProductOrderingSystem.CartService.Domain.Repositories;

namespace ProductOrderingSystem.CartService.Application.Commands.Carts;

public class CreateCartCommandHandler : IRequestHandler<CreateCartCommand, ErrorOr<CreateCartResponse>>
{
    private readonly ICartRepository _cartRepository;

    public CreateCartCommandHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<ErrorOr<CreateCartResponse>> Handle(CreateCartCommand request, CancellationToken cancellationToken)
    {
        // Check if customer already has a cart
        var existingCart = await _cartRepository.GetByCustomerIdAsync(request.CustomerId);
        if (existingCart != null)
        {
            return new CreateCartResponse(
                existingCart.Id,
                existingCart.CustomerId,
                existingCart.CustomerEmail,
                existingCart.CreatedAt
            );
        }

        // Create new cart
        var cart = new Cart(request.CustomerId, request.CustomerEmail);
        await _cartRepository.CreateAsync(cart);

        var response = new CreateCartResponse(
            cart.Id,
            cart.CustomerId,
            cart.CustomerEmail,
            cart.CreatedAt
        );

        return response;
    }
}

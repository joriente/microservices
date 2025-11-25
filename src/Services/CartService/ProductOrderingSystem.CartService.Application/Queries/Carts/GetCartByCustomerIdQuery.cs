using ErrorOr;
using MediatR;

namespace ProductOrderingSystem.CartService.Application.Queries.Carts;

public record GetCartByCustomerIdQuery(string CustomerId) : IRequest<ErrorOr<CartDto>>;

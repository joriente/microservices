using ErrorOr;

namespace ProductOrderingSystem.CartService.Application.Queries.Carts;

public record GetCartByCustomerIdQuery(string CustomerId);

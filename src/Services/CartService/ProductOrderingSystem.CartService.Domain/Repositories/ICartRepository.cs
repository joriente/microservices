using ProductOrderingSystem.CartService.Domain.Entities;

namespace ProductOrderingSystem.CartService.Domain.Repositories;

public interface ICartRepository
{
    Task<Cart?> GetByIdAsync(string cartId);
    Task<Cart?> GetByCustomerIdAsync(string customerId);
    Task<Cart> CreateAsync(Cart cart);
    Task UpdateAsync(Cart cart);
    Task DeleteAsync(string cartId);
}

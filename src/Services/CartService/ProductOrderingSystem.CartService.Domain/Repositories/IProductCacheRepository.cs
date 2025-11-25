using ProductOrderingSystem.CartService.Domain.Entities;

namespace ProductOrderingSystem.CartService.Domain.Repositories;

/// <summary>
/// Repository for managing cached product data in Cart Service.
/// Used to validate cart items and ensure accurate pricing without calling Product Service API.
/// </summary>
public interface IProductCacheRepository
{
    Task<ProductCacheEntry?> GetByIdAsync(string productId);
    Task UpsertAsync(ProductCacheEntry product, CancellationToken cancellationToken = default);
    Task DeleteAsync(string productId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string productId);
}

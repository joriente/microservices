using ProductOrderingSystem.OrderService.Domain.Entities;

namespace ProductOrderingSystem.OrderService.Domain.Repositories;

/// <summary>
/// Repository for managing cached product data in Order Service.
/// This cache is synchronized via events from Product Service.
/// </summary>
public interface IProductCacheRepository
{
    /// <summary>
    /// Gets a cached product by ID
    /// </summary>
    Task<ProductCacheEntry?> GetByIdAsync(string productId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets multiple cached products by their IDs
    /// </summary>
    Task<IEnumerable<ProductCacheEntry>> GetByIdsAsync(IEnumerable<string> productIds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Inserts or updates a cached product entry
    /// </summary>
    Task UpsertAsync(ProductCacheEntry product, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a cached product entry (when product is deleted from Product Service)
    /// </summary>
    Task DeleteAsync(string productId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all active products from cache
    /// </summary>
    Task<IEnumerable<ProductCacheEntry>> GetActiveProductsAsync(CancellationToken cancellationToken = default);
}

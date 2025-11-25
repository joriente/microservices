using MongoDB.Driver;
using ProductOrderingSystem.OrderService.Domain.Entities;
using ProductOrderingSystem.OrderService.Domain.Repositories;
using ProductOrderingSystem.OrderService.Infrastructure.Persistence;

namespace ProductOrderingSystem.OrderService.Infrastructure.Repositories;

public class ProductCacheRepository : IProductCacheRepository
{
    private readonly IMongoCollection<ProductCacheEntry> _collection;

    public ProductCacheRepository(OrderDbContext context)
    {
        _collection = context.ProductCache;
    }

    public async Task<ProductCacheEntry?> GetByIdAsync(string productId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ProductCacheEntry>.Filter.Eq(p => p.Id, productId);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<ProductCacheEntry>> GetByIdsAsync(IEnumerable<string> productIds, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ProductCacheEntry>.Filter.In(p => p.Id, productIds);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task UpsertAsync(ProductCacheEntry product, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ProductCacheEntry>.Filter.Eq(p => p.Id, product.Id);
        var options = new ReplaceOptions { IsUpsert = true };
        
        product.LastUpdated = DateTime.UtcNow;
        
        await _collection.ReplaceOneAsync(filter, product, options, cancellationToken);
    }

    public async Task DeleteAsync(string productId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ProductCacheEntry>.Filter.Eq(p => p.Id, productId);
        await _collection.DeleteOneAsync(filter, cancellationToken);
    }

    public async Task<IEnumerable<ProductCacheEntry>> GetActiveProductsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<ProductCacheEntry>.Filter.Eq(p => p.IsActive, true);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }
}

using MongoDB.Driver;
using ProductOrderingSystem.CartService.Domain.Entities;
using ProductOrderingSystem.CartService.Domain.Repositories;

namespace ProductOrderingSystem.CartService.Infrastructure.Repositories;

public class ProductCacheRepository : IProductCacheRepository
{
    private readonly IMongoCollection<ProductCacheEntry> _collection;

    public ProductCacheRepository(IMongoClient mongoClient)
    {
        var database = mongoClient.GetDatabase("cartdb");
        _collection = database.GetCollection<ProductCacheEntry>("product_cache");

        // Note: No need to create index on Id - MongoDB automatically creates _id index
    }

    public async Task<ProductCacheEntry?> GetByIdAsync(string productId)
    {
        return await _collection.Find(p => p.Id == productId).FirstOrDefaultAsync();
    }

    public async Task UpsertAsync(ProductCacheEntry product, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ProductCacheEntry>.Filter.Eq(p => p.Id, product.Id);
        var options = new ReplaceOptions { IsUpsert = true };
        await _collection.ReplaceOneAsync(filter, product, options, cancellationToken);
    }

    public async Task DeleteAsync(string productId, CancellationToken cancellationToken = default)
    {
        await _collection.DeleteOneAsync(p => p.Id == productId, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string productId)
    {
        return await _collection.Find(p => p.Id == productId).AnyAsync();
    }
}

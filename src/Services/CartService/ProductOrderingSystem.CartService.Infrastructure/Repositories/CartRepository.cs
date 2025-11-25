using MongoDB.Driver;
using ProductOrderingSystem.CartService.Domain.Entities;
using ProductOrderingSystem.CartService.Domain.Repositories;

namespace ProductOrderingSystem.CartService.Infrastructure.Repositories;

public class CartRepository : ICartRepository
{
    private readonly IMongoCollection<Cart> _carts;

    public CartRepository(IMongoClient mongoClient)
    {
        var database = mongoClient.GetDatabase("cartdb");
        _carts = database.GetCollection<Cart>("carts");

        // Create indexes
        var indexKeysDefinition = Builders<Cart>.IndexKeys.Ascending(c => c.CustomerId);
        var indexModel = new CreateIndexModel<Cart>(indexKeysDefinition);
        _carts.Indexes.CreateOneAsync(indexModel);
    }

    public async Task<Cart?> GetByIdAsync(string cartId)
    {
        return await _carts.Find(c => c.Id == cartId).FirstOrDefaultAsync();
    }

    public async Task<Cart?> GetByCustomerIdAsync(string customerId)
    {
        return await _carts.Find(c => c.CustomerId == customerId).FirstOrDefaultAsync();
    }

    public async Task<Cart> CreateAsync(Cart cart)
    {
        await _carts.InsertOneAsync(cart);
        return cart;
    }

    public async Task UpdateAsync(Cart cart)
    {
        await _carts.ReplaceOneAsync(c => c.Id == cart.Id, cart);
    }

    public async Task DeleteAsync(string cartId)
    {
        await _carts.DeleteOneAsync(c => c.Id == cartId);
    }
}

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ProductOrderingSystem.OrderService.Domain.Entities;

namespace ProductOrderingSystem.OrderService.Infrastructure.Persistence;

public class OrderDbContext
{
    private readonly IMongoDatabase _database;
    
    public OrderDbContext(IMongoClient mongoClient, IOptions<OrderDatabaseSettings> settings)
    {
        _database = mongoClient.GetDatabase(settings.Value.DatabaseName);
    }
    
    public IMongoCollection<Order> Orders => _database.GetCollection<Order>("orders");
    
    public IMongoCollection<ProductCacheEntry> ProductCache => _database.GetCollection<ProductCacheEntry>("productcache");
}

public class OrderDatabaseSettings
{
    public string DatabaseName { get; set; } = "productorderingdb";
}
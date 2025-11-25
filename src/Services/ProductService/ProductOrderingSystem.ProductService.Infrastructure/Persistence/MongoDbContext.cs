using MongoDB.Driver;
using ProductOrderingSystem.ProductService.Infrastructure.Configuration;

namespace ProductOrderingSystem.ProductService.Infrastructure.Persistence;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    
    public MongoDbContext(IMongoClient mongoClient, string databaseName)
    {
        _database = mongoClient.GetDatabase(databaseName);
    }

    public MongoDbContext(IMongoClient mongoClient, MongoDbConfiguration configuration)
        : this(mongoClient, configuration.DatabaseName)
    {
    }

    public IMongoCollection<T> GetCollection<T>(string name)
    {
        return _database.GetCollection<T>(name);
    }
}
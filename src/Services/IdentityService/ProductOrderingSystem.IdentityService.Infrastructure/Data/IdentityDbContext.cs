using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using ProductOrderingSystem.IdentityService.Domain.Entities;

namespace ProductOrderingSystem.IdentityService.Infrastructure.Data;

public class IdentityDbContext
{
    private readonly IMongoDatabase _database;

    public IdentityDbContext(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("IdentityDb") 
            ?? throw new InvalidOperationException("IdentityDb connection string not found");
        
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase("identitydb");
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
}

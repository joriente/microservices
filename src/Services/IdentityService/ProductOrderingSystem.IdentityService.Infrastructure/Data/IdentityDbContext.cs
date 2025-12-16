using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using ProductOrderingSystem.IdentityService.Domain.Entities;

namespace ProductOrderingSystem.IdentityService.Infrastructure.Data;

public class IdentityDbContext
{
    private readonly IMongoDatabase _database;

    public IdentityDbContext(IMongoDatabase database)
    {
        _database = database;
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
}

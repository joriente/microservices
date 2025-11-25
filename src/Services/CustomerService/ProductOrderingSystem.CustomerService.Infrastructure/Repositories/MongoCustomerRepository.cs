using MongoDB.Driver;
using ProductOrderingSystem.CustomerService.Domain.Entities;
using ProductOrderingSystem.CustomerService.Domain.Repositories;

namespace ProductOrderingSystem.CustomerService.Infrastructure.Repositories;

public class MongoCustomerRepository : ICustomerRepository
{
    private readonly IMongoCollection<Customer> _collection;

    public MongoCustomerRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Customer>("customers");
        
        // Create indexes
        CreateIndexes();
    }

    private void CreateIndexes()
    {
        var emailIndexKeys = Builders<Customer>.IndexKeys.Ascending(c => c.Email);
        var emailIndexModel = new CreateIndexModel<Customer>(
            emailIndexKeys,
            new CreateIndexOptions { Unique = true });

        _collection.Indexes.CreateOne(emailIndexModel);
    }

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(c => c.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(c => c.Email == email.ToLowerInvariant())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<Customer>> GetAllAsync(int skip = 0, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(_ => true)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<Customer> AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(customer, cancellationToken: cancellationToken);
        return customer;
    }

    public async Task<Customer> UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await _collection.ReplaceOneAsync(
            c => c.Id == customer.Id,
            customer,
            cancellationToken: cancellationToken);
        return customer;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _collection.DeleteOneAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(c => c.Id == id)
            .AnyAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(c => c.Email == email.ToLowerInvariant())
            .AnyAsync(cancellationToken);
    }

    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _collection.CountDocumentsAsync(_ => true, cancellationToken: cancellationToken);
    }
}

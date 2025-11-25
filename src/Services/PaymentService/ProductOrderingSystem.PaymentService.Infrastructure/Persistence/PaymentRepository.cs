using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ProductOrderingSystem.PaymentService.Domain.Entities;
using ProductOrderingSystem.PaymentService.Domain.Repositories;
using ProductOrderingSystem.PaymentService.Infrastructure.Configuration;

namespace ProductOrderingSystem.PaymentService.Infrastructure.Persistence;

public class PaymentRepository : IPaymentRepository
{
    private readonly IMongoCollection<Payment> _payments;

    public PaymentRepository(IOptions<MongoDbSettings> settings)
    {
        var mongoClient = new MongoClient(settings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(settings.Value.DatabaseName);
        _payments = mongoDatabase.GetCollection<Payment>("Payments");

        // Create indexes
        CreateIndexes();
    }

    private void CreateIndexes()
    {
        // Index on OrderId for faster lookups
        var orderIdIndexModel = new CreateIndexModel<Payment>(
            Builders<Payment>.IndexKeys.Ascending(p => p.OrderId));
        
        // Index on UserId for user payment history
        var userIdIndexModel = new CreateIndexModel<Payment>(
            Builders<Payment>.IndexKeys.Ascending(p => p.UserId));
        
        // Index on Status for filtering
        var statusIndexModel = new CreateIndexModel<Payment>(
            Builders<Payment>.IndexKeys.Ascending(p => p.Status));

        _payments.Indexes.CreateMany(new[]
        {
            orderIdIndexModel,
            userIdIndexModel,
            statusIndexModel
        });
    }

    public async Task<Payment?> GetByIdAsync(Guid id)
    {
        return await _payments.Find(p => p.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<Payment>> GetByOrderIdAsync(Guid orderId)
    {
        return await _payments.Find(p => p.OrderId == orderId).ToListAsync();
    }

    public async Task<List<Payment>> GetByUserIdAsync(Guid userId)
    {
        return await _payments.Find(p => p.UserId == userId)
            .SortByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Payment> CreateAsync(Payment payment)
    {
        await _payments.InsertOneAsync(payment);
        return payment;
    }

    public async Task<Payment> UpdateAsync(Payment payment)
    {
        await _payments.ReplaceOneAsync(p => p.Id == payment.Id, payment);
        return payment;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _payments.Find(p => p.Id == id).AnyAsync();
    }
}

using MongoDB.Driver;
using ProductOrderingSystem.OrderService.Domain.Entities;
using ProductOrderingSystem.OrderService.Domain.Repositories;
using ProductOrderingSystem.OrderService.Infrastructure.Persistence;

namespace ProductOrderingSystem.OrderService.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;

    public OrderRepository(OrderDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Find(order => order.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Order>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Find(order => order.CustomerId == customerId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Order>> GetAllAsync(int skip = 0, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Find(_ => true)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Order> Orders, int TotalCount)> GetOrdersAsync(
        string? customerId = null,
        OrderStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        // Build filter
        var filterBuilder = Builders<Order>.Filter;
        var filters = new List<FilterDefinition<Order>>();

        if (!string.IsNullOrWhiteSpace(customerId))
            filters.Add(filterBuilder.Eq(o => o.CustomerId, customerId));

        if (status.HasValue)
            filters.Add(filterBuilder.Eq(o => o.Status, status.Value));

        if (startDate.HasValue)
            filters.Add(filterBuilder.Gte(o => o.CreatedAt, startDate.Value));

        if (endDate.HasValue)
            filters.Add(filterBuilder.Lte(o => o.CreatedAt, endDate.Value));

        var filter = filters.Count > 0
            ? filterBuilder.And(filters)
            : filterBuilder.Empty;

        // Get total count
        var totalCount = (int)await _context.Orders.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        // Get paginated results
        var skip = (page - 1) * pageSize;
        var orders = await _context.Orders
            .Find(filter)
            .SortByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return (orders, totalCount);
    }

    public async Task<Order> CreateAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.InsertOneAsync(order, null, cancellationToken);
        return order;
    }

    public async Task<Order> UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        order.UpdatedAt = DateTime.UtcNow;
        await _context.Orders.ReplaceOneAsync(
            o => o.Id == order.Id,
            order,
            cancellationToken: cancellationToken);
        return order;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await _context.Orders.DeleteOneAsync(
            order => order.Id == id,
            cancellationToken);
        return result.DeletedCount > 0;
    }

    public async Task<long> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Orders.CountDocumentsAsync(
            _ => true,
            cancellationToken: cancellationToken);
    }

    public async Task<long> GetCountByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders.CountDocumentsAsync(
            order => order.CustomerId == customerId,
            cancellationToken: cancellationToken);
    }
}
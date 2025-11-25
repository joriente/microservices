using ProductOrderingSystem.OrderService.Domain.Entities;

namespace ProductOrderingSystem.OrderService.Domain.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetAllAsync(int skip = 0, int limit = 50, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Order> Orders, int TotalCount)> GetOrdersAsync(
        string? customerId = null,
        OrderStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
    Task<Order> CreateAsync(Order order, CancellationToken cancellationToken = default);
    Task<Order> UpdateAsync(Order order, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<long> GetCountAsync(CancellationToken cancellationToken = default);
    Task<long> GetCountByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default);
}
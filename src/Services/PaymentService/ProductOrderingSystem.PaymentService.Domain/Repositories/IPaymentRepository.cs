using ProductOrderingSystem.PaymentService.Domain.Entities;

namespace ProductOrderingSystem.PaymentService.Domain.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id);
    Task<List<Payment>> GetByOrderIdAsync(Guid orderId);
    Task<List<Payment>> GetByUserIdAsync(Guid userId);
    Task<Payment> CreateAsync(Payment payment);
    Task<Payment> UpdateAsync(Payment payment);
    Task<bool> ExistsAsync(Guid id);
}

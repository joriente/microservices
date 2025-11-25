using ErrorOr;

namespace ProductOrderingSystem.PaymentService.Domain.Services;

public interface IStripePaymentService
{
    Task<ErrorOr<string>> CreatePaymentIntentAsync(
        decimal amount,
        string currency,
        Guid orderId,
        Guid userId,
        CancellationToken cancellationToken = default);
    
    Task<ErrorOr<Success>> ConfirmPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default);
    
    Task<ErrorOr<Success>> RefundPaymentAsync(
        string paymentIntentId,
        decimal amount,
        string currency,
        string reason,
        CancellationToken cancellationToken = default);
    
    Task<ErrorOr<string>> GetPaymentStatusAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default);
}

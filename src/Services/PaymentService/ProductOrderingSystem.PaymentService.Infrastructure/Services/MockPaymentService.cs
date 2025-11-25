using ErrorOr;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.PaymentService.Domain.Services;

namespace ProductOrderingSystem.PaymentService.Infrastructure.Services;

/// <summary>
/// Mock payment service for POC/testing purposes.
/// Simulates successful payment processing without requiring real Stripe API keys.
/// </summary>
public class MockPaymentService : IStripePaymentService
{
    private readonly ILogger<MockPaymentService> _logger;

    public MockPaymentService(ILogger<MockPaymentService> logger)
    {
        _logger = logger;
    }

    public Task<ErrorOr<string>> CreatePaymentIntentAsync(
        decimal amount,
        string currency,
        Guid orderId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Generate a mock payment intent ID
        var paymentIntentId = $"pi_mock_{Guid.NewGuid():N}";

        _logger.LogInformation(
            "✓ MOCK: Created PaymentIntent {PaymentIntentId} for Order {OrderId}, Amount: {Amount} {Currency}",
            paymentIntentId,
            orderId,
            amount,
            currency);

        return Task.FromResult<ErrorOr<string>>(paymentIntentId);
    }

    public Task<ErrorOr<Success>> ConfirmPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "✓ MOCK: Confirmed PaymentIntent {PaymentIntentId} - Payment Successful",
            paymentIntentId);

        return Task.FromResult<ErrorOr<Success>>(Result.Success);
    }

    public Task<ErrorOr<Success>> RefundPaymentAsync(
        string paymentIntentId,
        decimal amount,
        string currency,
        string reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "✓ MOCK: Refunded PaymentIntent {PaymentIntentId}, Amount: {Amount} {Currency}, Reason: {Reason}",
            paymentIntentId,
            amount,
            currency,
            reason);

        return Task.FromResult<ErrorOr<Success>>(Result.Success);
    }

    public Task<ErrorOr<string>> GetPaymentStatusAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "✓ MOCK: Payment status for {PaymentIntentId}: succeeded",
            paymentIntentId);

        return Task.FromResult<ErrorOr<string>>("succeeded");
    }
}

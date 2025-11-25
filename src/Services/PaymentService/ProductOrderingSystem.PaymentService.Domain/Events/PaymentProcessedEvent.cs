namespace ProductOrderingSystem.PaymentService.Domain.Events;

public record PaymentProcessedEvent(
    Guid PaymentId,
    Guid OrderId,
    string StripePaymentIntentId,
    decimal Amount,
    string Currency,
    DateTime ProcessedAt);

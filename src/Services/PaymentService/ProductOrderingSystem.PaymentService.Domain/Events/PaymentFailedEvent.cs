namespace ProductOrderingSystem.PaymentService.Domain.Events;

public record PaymentFailedEvent(
    Guid PaymentId,
    Guid OrderId,
    string Reason,
    DateTime FailedAt);

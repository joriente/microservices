namespace ProductOrderingSystem.PaymentService.Domain.Events;

public record PaymentCreatedEvent(
    Guid PaymentId,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string Currency,
    DateTime CreatedAt);

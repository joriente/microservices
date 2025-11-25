namespace ProductOrderingSystem.PaymentService.Domain.Events;

public record PaymentRefundedEvent(
    Guid PaymentId,
    Guid OrderId,
    decimal RefundAmount,
    string Currency,
    string Reason,
    DateTime RefundedAt);

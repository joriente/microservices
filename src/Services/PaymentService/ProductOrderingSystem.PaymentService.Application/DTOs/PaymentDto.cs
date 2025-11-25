namespace ProductOrderingSystem.PaymentService.Application.DTOs;

public record PaymentDto(
    Guid Id,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string Currency,
    string Status,
    string? StripePaymentIntentId,
    string? FailureReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? CompletedAt);

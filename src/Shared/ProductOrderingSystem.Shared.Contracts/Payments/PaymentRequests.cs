namespace ProductOrderingSystem.Shared.Contracts.Payments;

public record RefundRequest(string Reason);

public record ProcessPaymentRequest(
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string Currency);

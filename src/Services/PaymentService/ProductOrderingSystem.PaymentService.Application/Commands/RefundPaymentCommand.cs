using ErrorOr;

namespace ProductOrderingSystem.PaymentService.Application.Commands;

public record RefundPaymentCommand(
    Guid PaymentId,
    string Reason);

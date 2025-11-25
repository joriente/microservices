using ErrorOr;
using MediatR;

namespace ProductOrderingSystem.PaymentService.Application.Commands;

public record RefundPaymentCommand(
    Guid PaymentId,
    string Reason) : IRequest<ErrorOr<Success>>;

using ErrorOr;
using ProductOrderingSystem.PaymentService.Application.DTOs;

namespace ProductOrderingSystem.PaymentService.Application.Commands;

public record ProcessPaymentCommand(
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string Currency);

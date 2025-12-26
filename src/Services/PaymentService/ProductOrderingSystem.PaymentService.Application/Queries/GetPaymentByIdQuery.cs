using ErrorOr;
using ProductOrderingSystem.PaymentService.Application.DTOs;

namespace ProductOrderingSystem.PaymentService.Application.Queries;

public record GetPaymentByIdQuery(Guid PaymentId);

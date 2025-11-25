using ErrorOr;
using MediatR;
using ProductOrderingSystem.PaymentService.Application.DTOs;

namespace ProductOrderingSystem.PaymentService.Application.Queries;

public record GetPaymentByIdQuery(Guid PaymentId) : IRequest<ErrorOr<PaymentDto>>;

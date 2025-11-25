using ErrorOr;
using MediatR;
using ProductOrderingSystem.PaymentService.Application.DTOs;

namespace ProductOrderingSystem.PaymentService.Application.Queries;

public record GetPaymentsByOrderIdQuery(Guid OrderId) : IRequest<ErrorOr<List<PaymentDto>>>;

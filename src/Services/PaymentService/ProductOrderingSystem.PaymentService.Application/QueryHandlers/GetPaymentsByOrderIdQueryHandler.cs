using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.PaymentService.Application.DTOs;
using ProductOrderingSystem.PaymentService.Application.Queries;
using ProductOrderingSystem.PaymentService.Domain.Repositories;

namespace ProductOrderingSystem.PaymentService.Application.QueryHandlers;

public class GetPaymentsByOrderIdQueryHandler : IRequestHandler<GetPaymentsByOrderIdQuery, ErrorOr<List<PaymentDto>>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<GetPaymentsByOrderIdQueryHandler> _logger;

    public GetPaymentsByOrderIdQueryHandler(
        IPaymentRepository paymentRepository,
        ILogger<GetPaymentsByOrderIdQueryHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    public async Task<ErrorOr<List<PaymentDto>>> Handle(GetPaymentsByOrderIdQuery request, CancellationToken cancellationToken)
    {
        var payments = await _paymentRepository.GetByOrderIdAsync(request.OrderId);
        
        var paymentDtos = payments.Select(payment => new PaymentDto(
            payment.Id,
            payment.OrderId,
            payment.UserId,
            payment.Amount.Amount,
            payment.Amount.Currency,
            payment.Status.ToString(),
            payment.StripePaymentIntentId,
            payment.FailureReason,
            payment.CreatedAt,
            payment.UpdatedAt,
            payment.CompletedAt)).ToList();

        return paymentDtos;
    }
}

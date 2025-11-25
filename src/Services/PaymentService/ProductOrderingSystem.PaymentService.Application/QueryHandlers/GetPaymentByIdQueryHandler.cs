using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.PaymentService.Application.DTOs;
using ProductOrderingSystem.PaymentService.Application.Queries;
using ProductOrderingSystem.PaymentService.Domain.Repositories;

namespace ProductOrderingSystem.PaymentService.Application.QueryHandlers;

public class GetPaymentByIdQueryHandler : IRequestHandler<GetPaymentByIdQuery, ErrorOr<PaymentDto>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<GetPaymentByIdQueryHandler> _logger;

    public GetPaymentByIdQueryHandler(
        IPaymentRepository paymentRepository,
        ILogger<GetPaymentByIdQueryHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    public async Task<ErrorOr<PaymentDto>> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(request.PaymentId);
        
        if (payment is null)
        {
            _logger.LogWarning("Payment {PaymentId} not found", request.PaymentId);
            return Error.NotFound("Payment.NotFound", $"Payment with ID {request.PaymentId} not found");
        }

        return new PaymentDto(
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
            payment.CompletedAt);
    }
}

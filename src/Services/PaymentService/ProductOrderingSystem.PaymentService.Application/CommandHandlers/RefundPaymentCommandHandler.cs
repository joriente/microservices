using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.PaymentService.Application.Commands;
using ProductOrderingSystem.PaymentService.Domain.Repositories;
using ProductOrderingSystem.PaymentService.Domain.Services;

namespace ProductOrderingSystem.PaymentService.Application.CommandHandlers;

public class RefundPaymentCommandHandler : IRequestHandler<RefundPaymentCommand, ErrorOr<Success>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IStripePaymentService _stripeService;
    private readonly ILogger<RefundPaymentCommandHandler> _logger;

    public RefundPaymentCommandHandler(
        IPaymentRepository paymentRepository,
        IStripePaymentService stripeService,
        ILogger<RefundPaymentCommandHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _stripeService = stripeService;
        _logger = logger;
    }

    public async Task<ErrorOr<Success>> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Refunding payment {PaymentId}, Reason: {Reason}",
            request.PaymentId,
            request.Reason);

        var payment = await _paymentRepository.GetByIdAsync(request.PaymentId);
        if (payment is null)
        {
            return Error.NotFound("Payment.NotFound", $"Payment with ID {request.PaymentId} not found");
        }

        if (string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
        {
            return Error.Validation("Payment.NoStripeId", "Payment does not have a Stripe PaymentIntent ID");
        }

        // Process refund with Stripe
        var refundResult = await _stripeService.RefundPaymentAsync(
            payment.StripePaymentIntentId,
            payment.Amount.Amount,
            payment.Amount.Currency,
            request.Reason,
            cancellationToken);

        if (refundResult.IsError)
        {
            _logger.LogError(
                "Failed to refund payment {PaymentId} with Stripe: {Errors}",
                request.PaymentId,
                string.Join(", ", refundResult.Errors.Select(e => e.Description)));
            return refundResult.Errors;
        }

        // Mark payment as refunded
        var markResult = payment.MarkAsRefunded(request.Reason);
        if (markResult.IsError)
        {
            return markResult.Errors;
        }

        await _paymentRepository.UpdateAsync(payment);

        _logger.LogInformation(
            "Successfully refunded payment {PaymentId}",
            request.PaymentId);

        return Result.Success;
    }
}

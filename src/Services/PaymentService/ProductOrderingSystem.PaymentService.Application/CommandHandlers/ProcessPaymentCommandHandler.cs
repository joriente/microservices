using ErrorOr;
using MassTransit;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.PaymentService.Application.Commands;
using ProductOrderingSystem.PaymentService.Application.DTOs;
using ProductOrderingSystem.PaymentService.Domain.Entities;
using ProductOrderingSystem.PaymentService.Domain.Repositories;
using ProductOrderingSystem.PaymentService.Domain.Services;
using ProductOrderingSystem.PaymentService.Domain.ValueObjects;
using SharedEvents = ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.PaymentService.Application.CommandHandlers;

public class ProcessPaymentCommandHandler
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IStripePaymentService _stripeService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;

    public ProcessPaymentCommandHandler(
        IPaymentRepository paymentRepository,
        IStripePaymentService stripeService,
        IPublishEndpoint publishEndpoint,
        ILogger<ProcessPaymentCommandHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _stripeService = stripeService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<ErrorOr<PaymentDto>> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing payment for Order {OrderId}, User {UserId}, Amount {Amount} {Currency}",
            request.OrderId,
            request.UserId,
            request.Amount,
            request.Currency);

        // Create Money value object
        var moneyResult = Money.Create(request.Amount, request.Currency);
        if (moneyResult.IsError)
        {
            return moneyResult.Errors;
        }

        // Create Payment entity
        var paymentResult = Payment.Create(request.OrderId, request.UserId, moneyResult.Value);
        if (paymentResult.IsError)
        {
            return paymentResult.Errors;
        }

        var payment = paymentResult.Value;

        // Save payment with Pending status
        await _paymentRepository.CreateAsync(payment);

        try
        {
            // Create Stripe PaymentIntent
            var paymentIntentResult = await _stripeService.CreatePaymentIntentAsync(
                request.Amount,
                request.Currency,
                request.OrderId,
                request.UserId,
                cancellationToken);

            if (paymentIntentResult.IsError)
            {
                // Mark payment as failed
                var failResult = payment.MarkAsFailed(
                    $"Failed to create payment intent: {string.Join(", ", paymentIntentResult.Errors.Select(e => e.Description))}");
                
                if (failResult.IsError)
                {
                    _logger.LogError("Failed to mark payment as failed: {Errors}", string.Join(", ", failResult.Errors));
                }

                await _paymentRepository.UpdateAsync(payment);

                // Publish PaymentFailedEvent
                await PublishPaymentFailedEvent(payment);

                return paymentIntentResult.Errors;
            }

            // Mark as processing with Stripe PaymentIntent ID
            var processingResult = payment.MarkAsProcessing(paymentIntentResult.Value);
            if (processingResult.IsError)
            {
                return processingResult.Errors;
            }

            await _paymentRepository.UpdateAsync(payment);

            // Note: In a real scenario with a frontend, we would return the client_secret
            // and let the frontend confirm the payment with Stripe Elements.
            // For this POC, we'll auto-confirm (which only works for test mode).
            
            _logger.LogInformation(
                "Payment {PaymentId} created and marked as processing with Stripe PaymentIntent {PaymentIntentId}",
                payment.Id,
                payment.StripePaymentIntentId);

            // Auto-confirm for POC (normally done by frontend)
            var confirmResult = await _stripeService.ConfirmPaymentIntentAsync(
                payment.StripePaymentIntentId!,
                cancellationToken);

            if (confirmResult.IsError)
            {
                var failResult = payment.MarkAsFailed(
                    $"Failed to confirm payment: {string.Join(", ", confirmResult.Errors.Select(e => e.Description))}");
                
                if (failResult.IsError)
                {
                    _logger.LogError("Failed to mark payment as failed: {Errors}", string.Join(", ", failResult.Errors));
                }

                await _paymentRepository.UpdateAsync(payment);
                await PublishPaymentFailedEvent(payment);

                return confirmResult.Errors;
            }

            // Mark as completed
            var completedResult = payment.MarkAsCompleted();
            if (completedResult.IsError)
            {
                return completedResult.Errors;
            }

            await _paymentRepository.UpdateAsync(payment);

            // Publish PaymentProcessedEvent
            await PublishPaymentProcessedEvent(payment);

            _logger.LogInformation(
                "Successfully processed payment {PaymentId} for Order {OrderId}",
                payment.Id,
                request.OrderId);

            return MapToDto(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for Order {OrderId}", request.OrderId);

            var failResult = payment.MarkAsFailed($"Unexpected error: {ex.Message}");
            if (failResult.IsError)
            {
                _logger.LogError("Failed to mark payment as failed: {Errors}", string.Join(", ", failResult.Errors));
            }

            await _paymentRepository.UpdateAsync(payment);
            await PublishPaymentFailedEvent(payment);

            return Error.Failure("Payment.ProcessingError", ex.Message);
        }
    }

    private async Task PublishPaymentProcessedEvent(Payment payment)
    {
        try
        {
            // Publish shared contract event for other services
            var sharedEvent = new SharedEvents.PaymentProcessedEvent(
                payment.Id,
                payment.OrderId,
                payment.UserId,
                payment.StripePaymentIntentId!,
                payment.Amount.Amount,
                payment.Amount.Currency,
                payment.Status.ToString(),
                payment.CompletedAt!.Value);

            await _publishEndpoint.Publish(sharedEvent);

            _logger.LogInformation(
                "Published PaymentProcessedEvent for Payment {PaymentId}, Order {OrderId}",
                payment.Id,
                payment.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish PaymentProcessedEvent for Payment {PaymentId}",
                payment.Id);
        }
    }

    private async Task PublishPaymentFailedEvent(Payment payment)
    {
        try
        {
            var paymentFailedEvent = new SharedEvents.PaymentFailedEvent(
                payment.Id,
                payment.OrderId,
                payment.UserId,
                payment.FailureReason ?? "Unknown error",
                DateTime.UtcNow);

            await _publishEndpoint.Publish(paymentFailedEvent);

            _logger.LogInformation(
                "Published PaymentFailedEvent for Payment {PaymentId}, Order {OrderId}",
                payment.Id,
                payment.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish PaymentFailedEvent for Payment {PaymentId}",
                payment.Id);
        }
    }

    private static PaymentDto MapToDto(Payment payment)
    {
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

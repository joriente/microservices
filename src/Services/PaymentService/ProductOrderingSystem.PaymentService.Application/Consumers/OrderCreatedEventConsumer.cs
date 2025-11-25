using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.PaymentService.Application.Commands;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.PaymentService.Application.Consumers;

public class OrderCreatedEventConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrderCreatedEventConsumer> _logger;

    public OrderCreatedEventConsumer(
        IMediator mediator,
        ILogger<OrderCreatedEventConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Received OrderCreatedEvent for Order {OrderId}, Customer {CustomerId}, Amount {TotalAmount}",
            message.OrderId,
            message.CustomerId,
            message.TotalAmount);

        try
        {
            // Process payment through MediatR command
            var command = new ProcessPaymentCommand(
                message.OrderId,
                message.CustomerId,
                message.TotalAmount,
                "USD"); // Default currency - could be part of the order event

            var result = await _mediator.Send(command);

            if (result.IsError)
            {
                _logger.LogError(
                    "Failed to process payment for Order {OrderId}: {Errors}",
                    message.OrderId,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                throw new Exception($"Payment processing failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            _logger.LogInformation(
                "Successfully processed payment {PaymentId} for Order {OrderId}",
                result.Value.Id,
                message.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing OrderCreatedEvent for Order {OrderId}",
                message.OrderId);
            throw; // Let MassTransit handle retry logic
        }
    }
}

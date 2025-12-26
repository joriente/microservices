using MassTransit;
using Wolverine;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.PaymentService.Application.Commands;
using ProductOrderingSystem.Shared.Contracts.Events;
using ProductOrderingSystem.PaymentService.Application.DTOs;
using ErrorOr;

namespace ProductOrderingSystem.PaymentService.Application.Consumers;

public class OrderCreatedEventConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<OrderCreatedEventConsumer> _logger;

    public OrderCreatedEventConsumer(
        IMessageBus messageBus,
        ILogger<OrderCreatedEventConsumer> logger)
    {
        _messageBus = messageBus;
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
            // Process payment through message bus
            var command = new ProcessPaymentCommand(
                message.OrderId,
                message.CustomerId,
                message.TotalAmount,
                "USD"); // Default currency - could be part of the order event

            var result = await _messageBus.InvokeAsync<ErrorOr<PaymentDto>>(command);

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

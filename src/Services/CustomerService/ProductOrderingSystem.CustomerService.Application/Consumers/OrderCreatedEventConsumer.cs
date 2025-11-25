using MassTransit;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.CustomerService.Domain.Repositories;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.CustomerService.Application.Consumers;

/// <summary>
/// Consumes OrderCreatedEvent to track customer order history
/// This is an example of event-driven data synchronization
/// </summary>
public class OrderCreatedEventConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<OrderCreatedEventConsumer> _logger;

    public OrderCreatedEventConsumer(
        ICustomerRepository customerRepository,
        ILogger<OrderCreatedEventConsumer> logger)
    {
        _customerRepository = customerRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Received OrderCreatedEvent for Customer {CustomerId}, Order {OrderId}",
            message.CustomerId,
            message.OrderId);

        try
        {
            // Verify customer exists
            var customer = await _customerRepository.GetByIdAsync(message.CustomerId);
            
            if (customer == null)
            {
                _logger.LogWarning(
                    "Customer {CustomerId} not found for Order {OrderId}",
                    message.CustomerId,
                    message.OrderId);
                return;
            }

            // In a real scenario, you might:
            // 1. Update customer's last order date
            // 2. Update customer segments/categories based on purchase patterns
            // 3. Trigger loyalty program updates
            // 4. Update customer lifetime value
            
            _logger.LogInformation(
                "Processed OrderCreatedEvent for Customer {CustomerId}",
                message.CustomerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing OrderCreatedEvent for Customer {CustomerId}, Order {OrderId}",
                message.CustomerId,
                message.OrderId);
            throw;
        }
    }
}

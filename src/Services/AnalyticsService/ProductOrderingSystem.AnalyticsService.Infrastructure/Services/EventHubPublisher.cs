using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.AnalyticsService.Application.Interfaces;

namespace ProductOrderingSystem.AnalyticsService.Infrastructure.Services;

public class EventHubPublisher : IEventHubPublisher, IAsyncDisposable
{
    private readonly EventHubProducerClient? _producerClient;
    private readonly ILogger<EventHubPublisher> _logger;
    private readonly bool _isEnabled;

    public EventHubPublisher(IConfiguration configuration, ILogger<EventHubPublisher> logger)
    {
        _logger = logger;
        var connectionString = configuration["EventHub:ConnectionString"];
        var eventHubName = configuration["EventHub:Name"];

        _logger.LogInformation("EventHub Configuration - ConnectionString: {HasConnectionString}, Name: {EventHubName}", 
            !string.IsNullOrEmpty(connectionString), eventHubName ?? "NULL");

        if (!string.IsNullOrEmpty(connectionString) && !string.IsNullOrEmpty(eventHubName))
        {
            try
            {
                _producerClient = new EventHubProducerClient(connectionString, eventHubName);
                _isEnabled = true;
                _logger.LogInformation("Event Hub publisher initialized for {EventHubName}", eventHubName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize Event Hub publisher. Events will only be stored locally.");
                _isEnabled = false;
            }
        }
        else
        {
            _logger.LogInformation("Event Hub configuration not found. Events will only be stored locally in PostgreSQL.");
            _isEnabled = false;
        }
    }

    public async Task PublishOrderEventAsync(Guid orderId, Guid customerId, decimal totalAmount, string status, int itemCount, CancellationToken cancellationToken = default)
    {
        if (!_isEnabled || _producerClient == null) return;

        try
        {
            var eventData = new
            {
                EventType = "OrderEvent",
                Timestamp = DateTime.UtcNow,
                Data = new
                {
                    OrderId = orderId,
                    CustomerId = customerId,
                    TotalAmount = totalAmount,
                    Status = status,
                    ItemCount = itemCount
                }
            };

            await PublishEventAsync(eventData, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish OrderEvent to Event Hub for Order {OrderId}", orderId);
        }
    }

    public async Task PublishPaymentEventAsync(Guid paymentId, Guid orderId, decimal amount, string status, string paymentMethod, CancellationToken cancellationToken = default)
    {
        if (!_isEnabled || _producerClient == null) return;

        try
        {
            var eventData = new
            {
                EventType = "PaymentEvent",
                Timestamp = DateTime.UtcNow,
                Data = new
                {
                    PaymentId = paymentId,
                    OrderId = orderId,
                    Amount = amount,
                    Status = status,
                    PaymentMethod = paymentMethod
                }
            };

            await PublishEventAsync(eventData, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish PaymentEvent to Event Hub for Payment {PaymentId}", paymentId);
        }
    }

    public async Task PublishProductEventAsync(Guid productId, string name, string category, decimal price, string eventType, CancellationToken cancellationToken = default)
    {
        if (!_isEnabled || _producerClient == null) return;

        try
        {
            var eventData = new
            {
                EventType = "ProductEvent",
                Timestamp = DateTime.UtcNow,
                Data = new
                {
                    ProductId = productId,
                    Name = name,
                    Category = category,
                    Price = price,
                    EventType = eventType
                }
            };

            await PublishEventAsync(eventData, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish ProductEvent to Event Hub for Product {ProductId}", productId);
        }
    }

    public async Task PublishInventoryEventAsync(Guid productId, Guid? orderId, int quantityChange, int quantityAfter, string eventType, CancellationToken cancellationToken = default)
    {
        if (!_isEnabled || _producerClient == null) return;

        try
        {
            var eventData = new
            {
                EventType = "InventoryEvent",
                Timestamp = DateTime.UtcNow,
                Data = new
                {
                    ProductId = productId,
                    OrderId = orderId,
                    QuantityChange = quantityChange,
                    QuantityAfter = quantityAfter,
                    EventType = eventType
                }
            };

            await PublishEventAsync(eventData, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish InventoryEvent to Event Hub for Product {ProductId}", productId);
        }
    }

    private async Task PublishEventAsync(object eventData, CancellationToken cancellationToken)
    {
        if (_producerClient == null) return;

        var json = JsonSerializer.Serialize(eventData);
        _logger.LogInformation("Publishing to Event Hub: {Json}", json);
        
        var eventDataBatch = await _producerClient.CreateBatchAsync(cancellationToken);

        if (!eventDataBatch.TryAdd(new EventData(json)))
        {
            throw new Exception("Event is too large for the batch");
        }

        await _producerClient.SendAsync(eventDataBatch, cancellationToken);
        _logger.LogDebug("Published event to Event Hub: {EventType}", eventData.GetType().GetProperty("EventType")?.GetValue(eventData));
    }

    public async ValueTask DisposeAsync()
    {
        if (_producerClient != null)
        {
            await _producerClient.DisposeAsync();
        }
    }
}

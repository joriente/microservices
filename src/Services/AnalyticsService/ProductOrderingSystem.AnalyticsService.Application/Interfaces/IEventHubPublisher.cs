namespace ProductOrderingSystem.AnalyticsService.Application.Interfaces;

public interface IEventHubPublisher
{
    Task PublishOrderEventAsync(Guid orderId, Guid customerId, decimal totalAmount, string status, int itemCount, CancellationToken cancellationToken = default);
    Task PublishPaymentEventAsync(Guid paymentId, Guid orderId, decimal amount, string status, string paymentMethod, CancellationToken cancellationToken = default);
    Task PublishProductEventAsync(Guid productId, string name, string category, decimal price, string eventType, CancellationToken cancellationToken = default);
    Task PublishInventoryEventAsync(Guid productId, Guid? orderId, int quantityChange, int quantityAfter, string eventType, CancellationToken cancellationToken = default);
}

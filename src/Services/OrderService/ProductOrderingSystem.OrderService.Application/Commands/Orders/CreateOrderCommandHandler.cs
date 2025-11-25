using ErrorOr;
using MassTransit;
using MediatR;
using ProductOrderingSystem.OrderService.Domain.Entities;
using ProductOrderingSystem.OrderService.Domain.Repositories;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.OrderService.Application.Commands.Orders;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, ErrorOr<Order>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductCacheRepository _productCacheRepository;
    private readonly IPublishEndpoint _publishEndpoint;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductCacheRepository productCacheRepository,
        IPublishEndpoint publishEndpoint)
    {
        _orderRepository = orderRepository;
        _productCacheRepository = productCacheRepository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<ErrorOr<Order>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.CustomerId))
            return Error.Validation("Order.CustomerId", "Customer ID is required");
        
        if (string.IsNullOrWhiteSpace(request.CustomerEmail))
            return Error.Validation("Order.CustomerEmail", "Customer email is required");
        
        if (string.IsNullOrWhiteSpace(request.CustomerName))
            return Error.Validation("Order.CustomerName", "Customer name is required");
        
        if (request.Items == null || !request.Items.Any())
            return Error.Validation("Order.Items", "Order must contain at least one item");

        // Validate and enrich order items using product cache
        var orderItems = new List<OrderItem>();
        
        foreach (var item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.ProductId))
                return Error.Validation("OrderItem.ProductId", "Product ID is required");
            
            if (item.Quantity <= 0)
                return Error.Validation("OrderItem.Quantity", "Quantity must be greater than zero");
            
            // Validate product exists in cache
            var cachedProduct = await _productCacheRepository.GetByIdAsync(item.ProductId, cancellationToken);
            if (cachedProduct == null)
            {
                return Error.NotFound(
                    "OrderItem.Product",
                    $"Product {item.ProductId} not found in product catalog");
            }
            
            // Validate product is active
            if (!cachedProduct.IsActive)
            {
                return Error.Validation(
                    "OrderItem.Product",
                    $"Product '{cachedProduct.Name}' is not active and cannot be ordered");
            }
            
            // Use cached product data (name and price) instead of client-provided data
            // This ensures data consistency and prevents price manipulation
            orderItems.Add(new OrderItem(
                productId: cachedProduct.Id,
                productName: cachedProduct.Name,
                unitPrice: cachedProduct.Price,
                quantity: item.Quantity
            ));
        }

        // Create order
        var order = new Order(
            request.CustomerId,
            request.CustomerEmail,
            request.CustomerName,
            orderItems,
            request.Notes
        );

        try
        {
            var createdOrder = await _orderRepository.CreateAsync(order, cancellationToken);
            
            // Publish OrderCreatedEvent to the message bus
            var orderCreatedEvent = new OrderCreatedEvent(
                OrderId: Guid.Parse(createdOrder.Id),
                CustomerId: Guid.Parse(createdOrder.CustomerId),
                Items: createdOrder.Items.Select(i => new OrderItemDto(
                    ProductId: Guid.Parse(i.ProductId),
                    Quantity: i.Quantity,
                    UnitPrice: i.UnitPrice
                )).ToList(),
                TotalAmount: createdOrder.TotalAmount,
                CreatedAt: createdOrder.CreatedAt
            );

            await _publishEndpoint.Publish(orderCreatedEvent, cancellationToken);
            
            return createdOrder;
        }
        catch (Exception ex)
        {
            // Only catch infrastructure exceptions (DB, messaging) - validation errors already returned above
            return Error.Failure("Order.Creation", $"Failed to create order: {ex.Message}");
        }
    }
}
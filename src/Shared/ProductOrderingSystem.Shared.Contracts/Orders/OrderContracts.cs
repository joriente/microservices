namespace ProductOrderingSystem.Shared.Contracts.Orders
{
    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Processing,
        Shipped,
        Delivered,
        Cancelled
    }

    public record OrderDto(
        string Id,
        string CustomerId,
        string CustomerEmail,
        string CustomerName,
        List<OrderItemDto> Items,
        decimal TotalAmount,
        OrderStatus Status,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        string? Notes
    );

    public record OrderItemDto(
        string ProductId,
        string ProductName,
        decimal Price,
        int Quantity,
        decimal SubTotal
    );

    public record CreateOrderRequest(
        string CustomerId,
        string CustomerEmail,
        string CustomerName,
        List<CreateOrderItemRequest> Items,
        string? Notes = null
    );

    public record CreateOrderItemRequest(
        string ProductId,
        string ProductName,
        decimal Price,
        int Quantity
    );
    // Note: ProductName and Price required until event-driven product sync is working

    public record UpdateOrderStatusRequest(
        string OrderId,
        OrderStatus Status
    );

    public record GetOrdersResponse(
        List<OrderDto> Orders,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages
    );

    // Pagination metadata for response header
    public record OrderPaginationMetadata(
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages,
        bool HasPrevious,
        bool HasNext
    );
}
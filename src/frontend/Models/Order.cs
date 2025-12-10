namespace ProductOrderingSystem.Web.Models;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}

public record Order(
    Guid Id,
    Guid CustomerId,
    string CustomerEmail,
    string CustomerName,
    List<OrderItem> Items,
    decimal TotalAmount,
    OrderStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? Notes);

public record OrderItem(
    Guid ProductId,
    string ProductName,
    decimal Price,
    int Quantity,
    decimal Subtotal);

public record CreateOrderRequest(
    Guid CustomerId,
    string CustomerEmail,
    string CustomerName,
    List<OrderItemRequest> Items,
    string? Notes);

public record OrderItemRequest(
    Guid ProductId,
    string ProductName,
    decimal Price,
    int Quantity);

public record PaginatedOrders(
    List<Order> Orders,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages)
{
    public PaginationMetadata PaginationData => new(Page, PageSize, TotalCount, TotalPages);
};

using ProductOrderingSystem.Web.Models;

namespace ProductOrderingSystem.Web.Services;

public interface IProductService
{
    Task<PaginatedProducts?> SearchProductsAsync(ProductSearchRequest request);
    Task<Product?> GetProductByIdAsync(Guid id);
    Task<Product?> CreateProductAsync(CreateProductRequest request);
    Task<Product?> UpdateProductAsync(Guid id, UpdateProductRequest request);
    Task<bool> DeleteProductAsync(Guid id);
}

public interface ICartService
{
    Task<Cart?> GetCartAsync();
    Task<Cart?> AddToCartAsync(AddToCartRequest request);
    Task<Cart?> UpdateCartItemAsync(Guid productId, int quantity);
    Task<bool> RemoveFromCartAsync(Guid productId);
    Task<bool> ClearCartAsync();
}

public interface IOrderService
{
    Task<PaginatedOrders?> GetOrdersAsync(int page = 1, int pageSize = 10);
    Task<Order?> GetOrderByIdAsync(Guid id);
    Task<Guid?> CreateOrderAsync(CreateOrderRequest request);
}

public interface ICustomerService
{
    Task<PaginatedCustomers?> GetCustomersAsync(int page = 1, int pageSize = 10);
    Task<Customer?> GetCustomerByIdAsync(Guid id);
    Task<Customer?> CreateCustomerAsync(CreateCustomerRequest request);
}

public interface IInventoryService
{
    Task<InventoryItem?> GetInventoryAsync(Guid productId);
    Task<List<Inventory>> GetAllInventoryAsync();
    Task<Inventory?> AdjustInventoryAsync(InventoryAdjustmentRequest request);
    Task<bool> ReserveInventoryAsync(ReserveInventoryRequest request);
}

public interface IPaymentService
{
    Task<PaymentResponse?> ProcessPaymentAsync(PaymentRequest request);
    Task<PaymentResponse?> ProcessPaymentAsync(ProcessPaymentRequest request);
}

using System.Net.Http.Json;
using ProductOrderingSystem.Web.Models;

namespace ProductOrderingSystem.Web.Services;

public class CartService : ICartService
{
    private readonly HttpClient _httpClient;

    public CartService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Cart?> GetCartAsync()
    {
        try
        {
            // API Gateway will extract customer ID from JWT token
            var response = await _httpClient.GetAsync("/api/carts/me");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine("GetCart: Cart not found (404)");
                return null; // Cart doesn't exist yet
            }
            response.EnsureSuccessStatusCode();
            var cart = await response.Content.ReadFromJsonAsync<Cart>();
            Console.WriteLine($"GetCart success: Cart ID={cart?.Id}, Items={cart?.Items?.Count}");
            return cart;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetCart exception: {ex.Message}");
            return null;
        }
    }

    public async Task<Cart?> AddToCartAsync(AddToCartRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/carts/items", request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"AddToCart failed: {response.StatusCode} - {error}");
                return null;
            }
            
            var cart = await response.Content.ReadFromJsonAsync<Cart>();
            Console.WriteLine($"AddToCart success: Cart ID={cart?.Id}, Items={cart?.Items?.Count}");
            return cart;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AddToCart exception: {ex.Message}");
            throw;
        }
    }

    public async Task<Cart?> UpdateCartItemAsync(Guid productId, int quantity)
    {
        var request = new UpdateCartItemRequest(productId.ToString(), quantity);
        // Note: We need the cartId for this, but we can get it from GetCartAsync first
        var cart = await GetCartAsync();
        if (cart == null) return null;
        
        var response = await _httpClient.PutAsJsonAsync($"/api/carts/{cart.Id}/items/{productId}", request);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<Cart>() : null;
    }

    public async Task<bool> RemoveFromCartAsync(Guid productId)
    {
        var cart = await GetCartAsync();
        if (cart == null) return false;
        
        var response = await _httpClient.DeleteAsync($"/api/carts/{cart.Id}/items/{productId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ClearCartAsync()
    {
        var cart = await GetCartAsync();
        if (cart == null) return false;
        
        var response = await _httpClient.DeleteAsync($"/api/carts/{cart.Id}");
        return response.IsSuccessStatusCode;
    }
}

public class OrderService : IOrderService
{
    private readonly HttpClient _httpClient;

    public OrderService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PaginatedOrders?> GetOrdersAsync(int page = 1, int pageSize = 10)
    {
        var response = await _httpClient.GetAsync($"/api/orders?page={page}&pageSize={pageSize}");
        if (!response.IsSuccessStatusCode)
            return null;

        var orders = await response.Content.ReadFromJsonAsync<List<Order>>();
        
        if (response.Headers.TryGetValues("Pagination", out var paginationValues))
        {
            var paginationJson = paginationValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(paginationJson))
            {
                var pagination = System.Text.Json.JsonSerializer.Deserialize<PaginationHeader>(paginationJson);
                if (pagination != null && orders != null)
                {
                    return new PaginatedOrders(orders, pagination.Page, pagination.PageSize, 
                        pagination.TotalCount, pagination.TotalPages);
                }
            }
        }

        return orders != null ? new PaginatedOrders(orders, 1, orders.Count, orders.Count, 1) : null;
    }

    public async Task<Order?> GetOrderByIdAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<Order>($"/api/orders/{id}");
    }

    public async Task<Guid?> CreateOrderAsync(CreateOrderRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/orders", request);
        
        Console.WriteLine($"CreateOrder - Status: {response.StatusCode}");
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"CreateOrder failed: {error}");
            return null;
        }
        
        // Order service returns 201 Created with Location header (REST pattern)
        // Extract order ID from Location and verify with GET request
        if (response.Headers.Location != null)
        {
            var locationUri = response.Headers.Location.ToString();
            Console.WriteLine($"CreateOrder - Location header: {locationUri}");
            
            // Extract order ID from Location header like "/api/orders/{guid}"
            var segments = locationUri.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var orderIdString = segments.LastOrDefault();
            
            if (!string.IsNullOrEmpty(orderIdString) && Guid.TryParse(orderIdString, out var orderId))
            {
                // Verify the order was created by doing a GET request
                Console.WriteLine($"CreateOrder - Verifying order {orderId} with GET request");
                
                try
                {
                    var verifyResponse = await _httpClient.GetAsync($"/api/orders/{orderId}");
                    if (verifyResponse.IsSuccessStatusCode)
                    {
                        var order = await verifyResponse.Content.ReadFromJsonAsync<Order>();
                        if (order != null)
                        {
                            Console.WriteLine($"CreateOrder success - OrderId: {orderId}, Status: {order.Status}");
                            return orderId;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"CreateOrder - GET verification failed: {verifyResponse.StatusCode}");
                        // Still return the ID even if verification fails - order was created
                        return orderId;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"CreateOrder - Verification error: {ex.Message}");
                    // Still return the ID even if verification fails - order was created
                    return orderId;
                }
            }
            
            Console.WriteLine($"CreateOrder - Could not parse OrderId from location: {orderIdString}");
        }
        else
        {
            Console.WriteLine("CreateOrder - No Location header in response");
        }
        
        return null;
    }
}

public class CustomerService : ICustomerService
{
    private readonly HttpClient _httpClient;

    public CustomerService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PaginatedCustomers?> GetCustomersAsync(int page = 1, int pageSize = 10)
    {
        var response = await _httpClient.GetAsync($"/api/customers?page={page}&pageSize={pageSize}");
        if (!response.IsSuccessStatusCode)
            return null;

        var customers = await response.Content.ReadFromJsonAsync<List<Customer>>();
        
        if (response.Headers.TryGetValues("Pagination", out var paginationValues))
        {
            var paginationJson = paginationValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(paginationJson))
            {
                var pagination = System.Text.Json.JsonSerializer.Deserialize<PaginationHeader>(paginationJson);
                if (pagination != null && customers != null)
                {
                    return new PaginatedCustomers(customers, pagination.Page, pagination.PageSize,
                        pagination.TotalCount, pagination.TotalPages);
                }
            }
        }

        return customers != null ? new PaginatedCustomers(customers, 1, customers.Count, customers.Count, 1) : null;
    }

    public async Task<Customer?> GetCustomerByIdAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<Customer>($"/api/customers/{id}");
    }

    public async Task<Customer?> CreateCustomerAsync(CreateCustomerRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/customers", request);
        if (response.IsSuccessStatusCode && response.Headers.Location != null)
        {
            var customerId = response.Headers.Location.ToString().Split('/').Last();
            return await GetCustomerByIdAsync(Guid.Parse(customerId));
        }
        return null;
    }
}

public class InventoryService : IInventoryService
{
    private readonly HttpClient _httpClient;

    public InventoryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<InventoryItem?> GetInventoryAsync(Guid productId)
    {
        return await _httpClient.GetFromJsonAsync<InventoryItem>($"/api/inventory/{productId}");
    }

    public async Task<List<Inventory>> GetAllInventoryAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<Inventory>>("/api/inventory") ?? new List<Inventory>();
    }

    public async Task<Inventory?> AdjustInventoryAsync(InventoryAdjustmentRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/inventory/adjust", request);
        return response.IsSuccessStatusCode 
            ? await response.Content.ReadFromJsonAsync<Inventory>() 
            : null;
    }

    public async Task<bool> ReserveInventoryAsync(ReserveInventoryRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/inventory/reserve", request);
        return response.IsSuccessStatusCode;
    }
}

public class PaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;

    public PaymentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PaymentResponse?> ProcessPaymentAsync(PaymentRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/payments", request);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<PaymentResponse>() : null;
    }

    public async Task<PaymentResponse?> ProcessPaymentAsync(ProcessPaymentRequest request)
    {
        // Log authentication header status
        var hasAuth = _httpClient.DefaultRequestHeaders.Authorization != null;
        Console.WriteLine($"ProcessPayment - Has Auth Header: {hasAuth}");
        if (hasAuth)
        {
            Console.WriteLine($"ProcessPayment - Auth Scheme: {_httpClient.DefaultRequestHeaders.Authorization?.Scheme}");
        }
        
        var response = await _httpClient.PostAsJsonAsync("/api/payments/process", request);
        
        Console.WriteLine($"ProcessPayment - Status: {response.StatusCode}");
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"ProcessPayment failed: {error}");
            return null;
        }
        
        if (response.IsSuccessStatusCode)
        {
            // Payment service returns PaymentDto, we need to map to PaymentResponse
            var result = await response.Content.ReadFromJsonAsync<PaymentDto>();
            if (result != null)
            {
                Console.WriteLine($"ProcessPayment success - PaymentId: {result.Id}, Status: {result.Status}");
                return new PaymentResponse(
                    result.Id,
                    result.OrderId,
                    result.Status,
                    result.TransactionId
                );
            }
        }
        return null;
    }

    // DTO to match backend response
    private record PaymentDto(
        Guid Id,
        Guid OrderId,
        string Status,
        string TransactionId,
        decimal Amount,
        string Currency);
}

using System.Net.Http.Json;
using ProductOrderingSystem.Web.Models;

namespace ProductOrderingSystem.Web.Services;

public class ProductService : IProductService
{
    private readonly HttpClient _httpClient;

    public ProductService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PaginatedProducts?> SearchProductsAsync(ProductSearchRequest request)
    {
        var query = $"?page={request.Page}&pageSize={request.PageSize}";
        if (!string.IsNullOrEmpty(request.SearchQuery))
            query += $"&search={Uri.EscapeDataString(request.SearchQuery)}";
        if (!string.IsNullOrEmpty(request.Category))
            query += $"&category={Uri.EscapeDataString(request.Category)}";
        if (request.MinPrice.HasValue)
            query += $"&minPrice={request.MinPrice}";
        if (request.MaxPrice.HasValue)
            query += $"&maxPrice={request.MaxPrice}";

        var response = await _httpClient.GetAsync($"/api/products{query}");
        if (!response.IsSuccessStatusCode)
            return null;

        var products = await response.Content.ReadFromJsonAsync<List<Product>>();
        
        // Parse pagination header
        if (response.Headers.TryGetValues("Pagination", out var paginationValues))
        {
            var paginationJson = paginationValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(paginationJson))
            {
                var pagination = System.Text.Json.JsonSerializer.Deserialize<PaginationHeader>(paginationJson);
                if (pagination != null && products != null)
                {
                    return new PaginatedProducts(
                        products,
                        pagination.Page,
                        pagination.PageSize,
                        pagination.TotalCount,
                        pagination.TotalPages);
                }
            }
        }

        return products != null ? new PaginatedProducts(products, 1, products.Count, products.Count, 1) : null;
    }

    public async Task<Product?> GetProductByIdAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<Product>($"/api/products/{id}");
    }

    public async Task<Product?> CreateProductAsync(CreateProductRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/products", request);
        if (response.IsSuccessStatusCode && response.Headers.Location != null)
        {
            var productId = response.Headers.Location.ToString().Split('/').Last();
            return await GetProductByIdAsync(Guid.Parse(productId));
        }
        return null;
    }

    public async Task<Product?> UpdateProductAsync(Guid id, UpdateProductRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/products/{id}", request);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<Product>() : null;
    }

    public async Task<bool> DeleteProductAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"/api/products/{id}");
        return response.IsSuccessStatusCode;
    }
}

internal record PaginationHeader(int Page, int PageSize, int TotalCount, int TotalPages);

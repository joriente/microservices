using ProductOrderingSystem.ProductService.Domain.Entities;

namespace ProductOrderingSystem.ProductService.Domain.Repositories
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(string id);
        Task<IEnumerable<Product>> GetAllAsync();
        Task<IEnumerable<Product>> GetByCategoryAsync(string category);
        Task<(IEnumerable<Product> Products, int TotalCount)> SearchAsync(
            string? searchTerm, 
            string? category, 
            decimal? minPrice, 
            decimal? maxPrice, 
            int page, 
            int pageSize);
        Task<Product> CreateAsync(Product product);
        Task<Product> UpdateAsync(Product product);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
    }
}
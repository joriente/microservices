namespace ProductOrderingSystem.ProductService.Infrastructure.Configuration
{
    public class MongoDbConfiguration
    {
        public string DatabaseName { get; set; } = string.Empty;
        public string ProductsCollectionName { get; set; } = "products";
    }
}
using MongoDB.Driver;
using ProductOrderingSystem.ProductService.Domain.Entities;
using ProductOrderingSystem.ProductService.Domain.Repositories;
using ProductOrderingSystem.ProductService.Infrastructure.Configuration;
using ProductOrderingSystem.ProductService.Infrastructure.Messaging;

namespace ProductOrderingSystem.ProductService.Infrastructure.Persistence
{
    public class ProductRepository : IProductRepository
    {
        private readonly IMongoCollection<Product> _products;
        private readonly DomainEventDispatcher _domainEventDispatcher;

        public ProductRepository(
            IMongoClient mongoClient, 
            MongoDbConfiguration config,
            DomainEventDispatcher domainEventDispatcher)
        {
            var database = mongoClient.GetDatabase(config.DatabaseName);
            _products = database.GetCollection<Product>(config.ProductsCollectionName);
            _domainEventDispatcher = domainEventDispatcher;

            // Create indexes
            CreateIndexes();
        }

        public async Task<Product?> GetByIdAsync(string id)
        {
            return await _products.Find(p => p.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _products.Find(_ => true).ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetByCategoryAsync(string category)
        {
            return await _products.Find(p => p.Category == category).ToListAsync();
        }

        public async Task<(IEnumerable<Product> Products, int TotalCount)> SearchAsync(
            string? searchTerm, 
            string? category, 
            decimal? minPrice, 
            decimal? maxPrice, 
            int page, 
            int pageSize)
        {
            var filterBuilder = Builders<Product>.Filter;
            var filter = filterBuilder.Empty;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchFilter = filterBuilder.Or(
                    filterBuilder.Regex(p => p.Name, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                    filterBuilder.Regex(p => p.Description, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"))
                );
                filter &= searchFilter;
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                filter &= filterBuilder.Eq(p => p.Category, category);
            }

            if (minPrice.HasValue)
            {
                filter &= filterBuilder.Gte(p => p.Price, minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                filter &= filterBuilder.Lte(p => p.Price, maxPrice.Value);
            }

            // Add active filter
            filter &= filterBuilder.Eq(p => p.IsActive, true);

            var totalCount = (int)await _products.CountDocumentsAsync(filter);

            var products = await _products
                .Find(filter)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .SortBy(p => p.Name)
                .ToListAsync();

            return (products, totalCount);
        }

        public async Task<Product> CreateAsync(Product product)
        {
            await _products.InsertOneAsync(product);
            
            // Log domain events before dispatching
            var domainEventCount = product.DomainEvents.Count;
            if (domainEventCount > 0)
            {
                Console.WriteLine($"[ProductRepository] Product {product.Id} has {domainEventCount} domain events to dispatch");
            }
            
            await _domainEventDispatcher.DispatchEventsAsync(product);
            return product;
        }

        public async Task<Product> UpdateAsync(Product product)
        {
            await _products.ReplaceOneAsync(p => p.Id == product.Id, product);
            
            // Log domain events before dispatching
            var domainEventCount = product.DomainEvents.Count;
            if (domainEventCount > 0)
            {
                Console.WriteLine($"[ProductRepository] Product {product.Id} has {domainEventCount} domain events to dispatch");
            }
            
            await _domainEventDispatcher.DispatchEventsAsync(product);
            return product;
        }

        public async Task DeleteAsync(string id)
        {
            await _products.DeleteOneAsync(p => p.Id == id);
        }

        public async Task<bool> ExistsAsync(string id)
        {
            var count = await _products.CountDocumentsAsync(p => p.Id == id);
            return count > 0;
        }

        private void CreateIndexes()
        {
            var indexKeysDefinition = Builders<Product>.IndexKeys
                .Ascending(p => p.Name)
                .Ascending(p => p.Category)
                .Ascending(p => p.Price);

            var indexModel = new CreateIndexModel<Product>(indexKeysDefinition);
            _products.Indexes.CreateOne(indexModel);

            // Text index for search
            var textIndexKeys = Builders<Product>.IndexKeys
                .Text(p => p.Name)
                .Text(p => p.Description);

            var textIndexModel = new CreateIndexModel<Product>(textIndexKeys);
            _products.Indexes.CreateOne(textIndexModel);
        }
    }
}
using ProductOrderingSystem.ProductService.Domain.Common;
using ProductOrderingSystem.ProductService.Domain.Events;

namespace ProductOrderingSystem.ProductService.Domain.Entities
{
    public class Product : BaseEntity
    {
        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public decimal Price { get; private set; }
        public int StockQuantity { get; private set; }
        public string Category { get; private set; } = string.Empty;
        public string ImageUrl { get; private set; } = string.Empty;
        public bool IsActive { get; private set; } = true;

        // Parameterless constructor for MongoDB
        private Product() { }

        public Product(string name, string description, decimal price, int stockQuantity, string category, string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Product name cannot be empty", nameof(name));
            
            if (price < 0)
                throw new ArgumentException("Price cannot be negative", nameof(price));
            
            if (stockQuantity < 0)
                throw new ArgumentException("Stock quantity cannot be negative", nameof(stockQuantity));

            Name = name;
            Description = description ?? string.Empty;
            Price = price;
            StockQuantity = stockQuantity;
            Category = category ?? string.Empty;
            ImageUrl = imageUrl ?? string.Empty;
            
            AddDomainEvent(new ProductCreatedEvent(Id, Name, Price, StockQuantity));
        }

        public void UpdateProduct(string name, string description, decimal price, int stockQuantity, string category, string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Product name cannot be empty", nameof(name));
            
            if (price < 0)
                throw new ArgumentException("Price cannot be negative", nameof(price));
            
            if (stockQuantity < 0)
                throw new ArgumentException("Stock quantity cannot be negative", nameof(stockQuantity));

            Name = name;
            Description = description ?? string.Empty;
            Price = price;
            StockQuantity = stockQuantity;
            Category = category ?? string.Empty;
            ImageUrl = imageUrl ?? string.Empty;
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new ProductUpdatedEvent(Id, Name, Price, StockQuantity));
        }

        public void UpdateStock(int quantity)
        {
            if (quantity < 0)
                throw new ArgumentException("Stock quantity cannot be negative", nameof(quantity));

            StockQuantity = quantity;
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new ProductStockUpdatedEvent(Id, StockQuantity));
        }

        public void ReserveStock(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive", nameof(quantity));
            
            if (StockQuantity < quantity)
                throw new InvalidOperationException($"Insufficient stock. Available: {StockQuantity}, Required: {quantity}");

            StockQuantity -= quantity;
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new ProductStockReservedEvent(Id, quantity, StockQuantity));
        }

        public void ReleaseStock(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive", nameof(quantity));

            StockQuantity += quantity;
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new ProductStockReleasedEvent(Id, quantity, StockQuantity));
        }

        /// <summary>
        /// Restores stock as part of compensation/saga pattern when order is cancelled.
        /// This is semantically the same as ReleaseStock but indicates a compensation action.
        /// </summary>
        public void RestoreStock(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive", nameof(quantity));

            StockQuantity += quantity;
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new ProductStockReleasedEvent(Id, quantity, StockQuantity));
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new ProductDeactivatedEvent(Id));
        }

        public void Activate()
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new ProductActivatedEvent(Id));
        }
    }
}
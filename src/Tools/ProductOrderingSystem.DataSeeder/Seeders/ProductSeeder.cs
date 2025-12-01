using Bogus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using ProductOrderingSystem.DataSeeder.Infrastructure;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.DataSeeder.Seeders;

public class ProductSeeder
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProductSeeder> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEventPublisher _eventPublisher;

    public ProductSeeder(
        IConfiguration configuration,
        ILogger<ProductSeeder> logger,
        IHttpClientFactory httpClientFactory,
        IEventPublisher eventPublisher)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _eventPublisher = eventPublisher;
    }

    public async Task SeedAsync()
    {
        var productCount = _configuration.GetValue<int>("Seeding:Services:ProductService:ProductCount", 100);
        var clearExisting = _configuration.GetValue<bool>("Seeding:ClearExistingData", false);
        var publishEvents = _configuration.GetValue<bool>("Seeding:Services:ProductService:PublishEvents", true);

        _logger.LogInformation("Product seeding configuration:");
        _logger.LogInformation("  - Product count: {Count}", productCount);
        _logger.LogInformation("  - Clear existing: {Clear}", clearExisting);
        _logger.LogInformation("  - Publish events: {PublishEvents}", publishEvents);

        // Connect directly to MongoDB for seeding (faster than HTTP)
        var mongoConnectionString = _configuration.GetConnectionString("MongoDB");
        var databaseName = _configuration.GetValue<string>("MongoDB:ProductDatabaseName", "productdb");
        var mongoClient = new MongoClient(mongoConnectionString);
        var database = mongoClient.GetDatabase(databaseName);
        var collection = database.GetCollection<ProductDocument>("products");

        // Check existing count
        var existingCount = await collection.CountDocumentsAsync(FilterDefinition<ProductDocument>.Empty);
        _logger.LogInformation("Current product count: {Count}", existingCount);

        if (existingCount >= productCount && !clearExisting)
        {
            _logger.LogInformation("Database already has {Count} products. Skipping seed.", existingCount);
            return;
        }

        if (clearExisting && existingCount > 0)
        {
            _logger.LogInformation("Clearing {Count} existing products...", existingCount);
            await collection.DeleteManyAsync(FilterDefinition<ProductDocument>.Empty);
            _logger.LogInformation("✓ Cleared existing products");
        }

        _logger.LogInformation("Generating {Count} products...", productCount);
        var products = GenerateProducts(productCount);

        _logger.LogInformation("Inserting products into MongoDB...");
        
        // Insert all products in bulk for better performance
        await collection.InsertManyAsync(products);
        _logger.LogInformation("✓ Successfully inserted {Count} products", productCount);

        // Publish events after successful insertion (if enabled)
        var publishedEvents = 0;
        if (publishEvents)
        {
            _logger.LogInformation("Publishing ProductCreatedEvent messages...");
            
            foreach (var product in products)
            {
                try
                {
                    var @event = new ProductCreatedEvent(
                        ProductId: product.Id,
                        Name: product.Name,
                        Price: product.Price,
                        StockQuantity: product.StockQuantity,
                        CreatedAt: product.CreatedAt
                    );

                    await _eventPublisher.PublishAsync(@event);
                    publishedEvents++;

                    // Log progress
                    if (publishedEvents % 10 == 0 || publishedEvents == productCount)
                    {
                        _logger.LogInformation("Published {Current}/{Total} events...", publishedEvents, productCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to publish event for product {ProductId}. Continuing...", product.Id);
                }
            }

            _logger.LogInformation("✓ Published {Count} ProductCreatedEvent messages", publishedEvents);
        }

        _logger.LogInformation("✓ Product seeding completed successfully");
    }

    private List<ProductDocument> GenerateProducts(int count)
    {
        var categories = new[]
        {
            "Electronics",
            "Clothing",
            "Books",
            "Home & Garden",
            "Sports",
            "Toys",
            "Food",
            "Health & Beauty"
        };

        var products = new List<ProductDocument>();
        var faker = new Faker();

        for (int i = 0; i < count; i++)
        {
            var category = faker.PickRandom(categories);
            var product = new ProductDocument
            {
                Id = Guid.NewGuid().ToString(),
                Name = GenerateProductName(faker, category),
                Description = GenerateProductDescription(faker, category),
                Price = GeneratePrice(faker, category),
                StockQuantity = faker.Random.Number(10, 200),
                Category = category,
                ImageUrl = GenerateImageUrl(faker, category),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            products.Add(product);

            if ((i + 1) % 10 == 0)
            {
                _logger.LogInformation("Generated {Current}/{Total} products...", i + 1, count);
            }
        }

        return products;
    }

    private string GenerateProductName(Faker faker, string category)
    {
        return category switch
        {
            "Electronics" => faker.PickRandom(new[]
            {
                "Wireless Bluetooth Headphones", "4K Ultra HD Smart TV", "Portable Power Bank",
                "Wireless Gaming Mouse", "USB-C Hub Adapter", "Smart Watch Pro",
                "Mechanical Keyboard", "Wireless Charging Pad", "Bluetooth Speaker",
                "Webcam HD 1080p", "Gaming Headset", "Laptop Stand",
                "Phone Screen Protector", "External SSD Drive", "Cable Management Kit"
            }) + $" {faker.Commerce.ProductAdjective()}",

            "Clothing" => faker.PickRandom(new[]
            {
                "Cotton T-Shirt", "Denim Jeans", "Hoodie Sweatshirt",
                "Running Shoes", "Leather Jacket", "Casual Sneakers",
                "Winter Coat", "Sports Shorts", "Polo Shirt",
                "Dress Shirt", "Yoga Pants", "Baseball Cap",
                "Canvas Backpack", "Wool Sweater", "Summer Dress"
            }) + $" - {faker.Commerce.Color()}",

            "Books" => faker.PickRandom(new[]
            {
                "The Art of Programming", "Mystery Novel Collection", "Self-Help Guide",
                "Science Fiction Epic", "Cookbook Collection", "Biography Series",
                "Fantasy Adventure", "History Encyclopedia", "Business Strategy",
                "Travel Guide", "Comic Book Series", "Poetry Anthology",
                "Children's Storybook", "Graphic Novel", "Technical Manual"
            }),

            "Home & Garden" => faker.PickRandom(new[]
            {
                "Indoor Plant Pot", "LED Desk Lamp", "Throw Pillow Set",
                "Coffee Table", "Garden Tool Set", "Wall Art Canvas",
                "Area Rug", "Storage Basket", "Picture Frame Set",
                "Candle Set", "Kitchen Organizer", "Bathroom Shelf"
            }) + $" {faker.Commerce.ProductAdjective()}",

            "Sports" => faker.PickRandom(new[]
            {
                "Yoga Mat", "Resistance Bands", "Dumbbells Set",
                "Running Belt", "Water Bottle", "Gym Bag",
                "Fitness Tracker", "Jump Rope", "Exercise Ball",
                "Tennis Racket", "Basketball", "Bicycle Helmet"
            }) + $" {faker.Commerce.ProductAdjective()}",

            "Toys" => faker.PickRandom(new[]
            {
                "Building Blocks Set", "Action Figure", "Board Game",
                "Puzzle Set", "Remote Control Car", "Stuffed Animal",
                "Art Supply Kit", "LEGO Set", "Toy Robot",
                "Educational Game", "Play-Doh Set", "Doll House"
            }) + $" {faker.Commerce.ProductAdjective()}",

            "Food" => faker.PickRandom(new[]
            {
                "Organic Coffee Beans", "Artisan Chocolate", "Gourmet Pasta",
                "Specialty Tea Collection", "Honey Jar", "Olive Oil",
                "Spice Set", "Protein Bars", "Trail Mix",
                "Energy Drinks Pack", "Dried Fruits", "Nuts Assortment"
            }) + $" {faker.Commerce.ProductAdjective()}",

            "Health & Beauty" => faker.PickRandom(new[]
            {
                "Moisturizer Cream", "Shampoo & Conditioner", "Face Mask Set",
                "Vitamin Supplements", "Essential Oils", "Body Lotion",
                "Lip Balm Set", "Nail Care Kit", "Hair Brush",
                "Makeup Palette", "Sunscreen SPF 50", "Hand Sanitizer Pack"
            }) + $" {faker.Commerce.ProductAdjective()}",

            _ => faker.Commerce.ProductName()
        };
    }

    private string GenerateProductDescription(Faker faker, string category)
    {
        var adjectives = new[] { "premium", "high-quality", "durable", "innovative", "stylish", "modern", "reliable" };
        var features = category switch
        {
            "Electronics" => new[] { "latest technology", "long battery life", "user-friendly interface", "wireless connectivity" },
            "Clothing" => new[] { "comfortable fit", "breathable fabric", "easy care", "versatile style" },
            "Books" => new[] { "engaging content", "well-researched", "beautifully illustrated", "thought-provoking" },
            "Home & Garden" => new[] { "space-saving design", "easy to clean", "weather-resistant", "eco-friendly materials" },
            "Sports" => new[] { "professional grade", "lightweight design", "enhanced performance", "ergonomic grip" },
            "Toys" => new[] { "safe for children", "educational value", "hours of fun", "develops creativity" },
            "Food" => new[] { "organic ingredients", "rich flavor", "nutritious", "ethically sourced" },
            "Health & Beauty" => new[] { "natural ingredients", "dermatologist tested", "cruelty-free", "effective results" },
            _ => new[] { "excellent quality", "great value", "highly rated", "customer favorite" }
        };

        return $"A {faker.PickRandom(adjectives)} product with {faker.PickRandom(features)}. " +
               $"{faker.Lorem.Sentence(10)} Perfect for everyday use.";
    }

    private decimal GeneratePrice(Faker faker, string category)
    {
        return category switch
        {
            "Electronics" => faker.Random.Decimal(29.99m, 999.99m),
            "Clothing" => faker.Random.Decimal(19.99m, 299.99m),
            "Books" => faker.Random.Decimal(9.99m, 49.99m),
            "Home & Garden" => faker.Random.Decimal(14.99m, 199.99m),
            "Sports" => faker.Random.Decimal(19.99m, 249.99m),
            "Toys" => faker.Random.Decimal(9.99m, 99.99m),
            "Food" => faker.Random.Decimal(4.99m, 49.99m),
            "Health & Beauty" => faker.Random.Decimal(9.99m, 89.99m),
            _ => faker.Random.Decimal(9.99m, 299.99m)
        };
    }

    private string GenerateImageUrl(Faker faker, string category)
    {
        // Use picsum.photos as a reliable placeholder image service
        // Each product gets a unique image based on a seed number
        var seed = faker.Random.Number(1, 1000);
        return $"https://picsum.photos/seed/{category.ToLower().Replace(" ", "")}{seed}/400/300";
    }
}

// MongoDB document model for direct insertion
public class ProductDocument
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string Category { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

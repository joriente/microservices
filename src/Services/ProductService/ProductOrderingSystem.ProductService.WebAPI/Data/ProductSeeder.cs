using Bogus;
using MediatR;
using ProductOrderingSystem.ProductService.Application.Commands.Products;
using ProductOrderingSystem.ProductService.Application.Queries.Products;
using ProductOrderingSystem.ProductService.Domain.Repositories;

namespace ProductOrderingSystem.ProductService.WebAPI.Data;

public class ProductSeeder : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProductSeeder> _logger;
    private readonly IConfiguration _configuration;

    public ProductSeeder(IServiceProvider serviceProvider, ILogger<ProductSeeder> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Check if seeding is enabled
        var seedingEnabled = _configuration.GetValue<bool>("Seeding:Enabled", false);
        if (!seedingEnabled)
        {
            _logger.LogInformation("ProductSeeder: Seeding is disabled in configuration. Use the separate DataSeeder project to seed data.");
            return;
        }

        // Wait longer for MongoDB and RabbitMQ to be fully ready
        _logger.LogInformation("ProductSeeder: Waiting for infrastructure to be ready...");
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            await SeedAsync(scope, mediator, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
        }
    }

    private async Task SeedAsync(IServiceScope scope, IMediator mediator, CancellationToken cancellationToken)
    {
        // Retry logic for infrastructure connection
        var maxRetries = 20;
        var retryDelay = TimeSpan.FromSeconds(5);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("ProductSeeder: Attempt {Attempt}/{MaxRetries} to seed products...", attempt, maxRetries);
                
                // Check if products already exist by querying the database
                var searchQuery = new SearchProductsQuery(
                    SearchTerm: null,
                    Category: null,
                    MinPrice: null,
                    MaxPrice: null,
                    Page: 1,
                    PageSize: 1
                );
                
                var searchResult = await mediator.Send(searchQuery, cancellationToken);
                
                if (searchResult.IsError)
                {
                    _logger.LogError("Error checking existing products: {Errors}", 
                        string.Join(", ", searchResult.Errors.Select(e => e.Description)));
                    return;
                }
                
                var expectedProductCount = _configuration.GetValue<int>("Seeding:ProductCount", 100);
                
                if (searchResult.Value.TotalCount >= expectedProductCount)
                {
                    _logger.LogInformation("Database already contains {Count} products (expected: {Expected}). Skipping seed.", 
                        searchResult.Value.TotalCount, expectedProductCount);
                    return;
                }
                
                if (searchResult.Value.TotalCount > 0)
                {
                    _logger.LogWarning("Database contains {Count} products but expected {Expected}. Clearing existing products and re-seeding...", 
                        searchResult.Value.TotalCount, expectedProductCount);
                    
                    // Get the repository to clear existing products
                    var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
                    var existingProducts = await productRepository.GetAllAsync();
                    foreach (var product in existingProducts)
                    {
                        await productRepository.DeleteAsync(product.Id);
                    }
                    _logger.LogInformation("Cleared {Count} existing products", searchResult.Value.TotalCount);
                }

                _logger.LogInformation("Seeding product database with sample data via MediatR commands...");

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

                var productData = GenerateProductData(100, categories);

                // Create products using MediatR commands - this will trigger domain events
                // Use CancellationToken.None to ensure seeding completes even if the application startup cancellation is triggered
                int successCount = 0;
                int failureCount = 0;
                foreach (var data in productData)
                {
                    try
                    {
                        var command = new CreateProductCommand(
                            data.Name,
                            data.Description,
                            data.Price,
                            data.Stock,
                            data.Category,
                            data.ImageUrl
                        );

                        // Don't use the cancellation token here to allow seeding to complete
                        var result = await mediator.Send(command, CancellationToken.None);
                        
                        if (!result.IsError)
                        {
                            successCount++;
                            if (successCount % 10 == 0)
                            {
                                _logger.LogInformation("Progress: {SuccessCount}/{TotalCount} products seeded", successCount, productData.Count);
                            }
                        }
                        else
                        {
                            failureCount++;
                            _logger.LogWarning("Failed to seed product {Name}: {Error}", data.Name, result.FirstError.Description);
                        }
                        
                        // Small delay to avoid overwhelming MongoDB
                        await Task.Delay(50, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        _logger.LogWarning(ex, "Exception seeding product {Name}", data.Name);
                    }
                }

                _logger.LogInformation("✓ Seeding completed: {SuccessCount} products created, {FailureCount} failures out of {TotalCount} total", 
                    successCount, failureCount, productData.Count);
                return;
            }
            catch (TimeoutException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning("Failed infrastructure check (timeout) on attempt {Attempt}/{MaxRetries}. Retrying in {Delay} seconds... Error: {Error}", 
                    attempt, maxRetries, retryDelay.TotalSeconds, ex.Message);
                await Task.Delay(retryDelay, cancellationToken);
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning("Failed infrastructure check on attempt {Attempt}/{MaxRetries}. Retrying in {Delay} seconds... Error: {Error}", 
                    attempt, maxRetries, retryDelay.TotalSeconds, ex.Message);
                await Task.Delay(retryDelay, cancellationToken);
            }
        }

        _logger.LogError("Failed to seed database after {MaxRetries} attempts. Infrastructure may not be available.", maxRetries);
    }

    private record ProductData(string Name, string Description, decimal Price, int Stock, string Category, string ImageUrl);

    private List<ProductData> GenerateProductData(int count, string[] categories)
    {
        var productDataList = new List<ProductData>();
        var random = new Random();
        
        for (int i = 0; i < count; i++)
        {
            var faker = new Faker();
            var category = faker.PickRandom(categories);
            var productName = GenerateProductName(faker, category);
            var description = GenerateProductDescription(faker, productName, category);
            var price = GeneratePrice(faker, category);
            var stock = faker.Random.Number(0, 200);
            var imageUrl = GenerateImageUrl(faker, category);
            
            var productData = new ProductData(productName, description, price, stock, category, imageUrl);
            productDataList.Add(productData);
        }

        return productDataList;
    }

    private string GenerateProductName(Faker f, string category)
    {
        return category switch
        {
            "Electronics" => f.PickRandom(new[]
            {
                "Wireless Bluetooth Headphones",
                "4K Ultra HD Smart TV",
                "Portable Power Bank",
                "Wireless Gaming Mouse",
                "USB-C Hub Adapter",
                "Smart Watch Pro",
                "Mechanical Keyboard",
                "Wireless Charging Pad",
                "Bluetooth Speaker",
                "Webcam HD 1080p",
                "Gaming Headset",
                "Laptop Stand",
                "Phone Screen Protector",
                "External SSD Drive",
                "Cable Management Kit"
            }) + $" {f.Commerce.ProductAdjective()}",

            "Clothing" => f.PickRandom(new[]
            {
                "Cotton T-Shirt",
                "Denim Jeans",
                "Hoodie Sweatshirt",
                "Running Shoes",
                "Leather Jacket",
                "Casual Sneakers",
                "Winter Coat",
                "Sports Shorts",
                "Polo Shirt",
                "Dress Shirt",
                "Yoga Pants",
                "Baseball Cap",
                "Canvas Backpack",
                "Wool Sweater",
                "Summer Dress"
            }) + $" - {f.Commerce.Color()}",

            "Books" => f.PickRandom(new[]
            {
                "The Art of Programming",
                "Mystery Novel Collection",
                "Self-Help Guide",
                "Science Fiction Epic",
                "Cookbook Collection",
                "Biography Series",
                "Fantasy Adventure",
                "History Encyclopedia",
                "Business Strategy",
                "Travel Guide",
                "Comic Book Series",
                "Poetry Anthology",
                "Children's Storybook",
                "Graphic Novel",
                "Technical Manual"
            }),

            "Home & Garden" => f.PickRandom(new[]
            {
                "Indoor Plant Pot",
                "LED Desk Lamp",
                "Throw Pillow Set",
                "Coffee Table",
                "Garden Tool Set",
                "Wall Clock",
                "Storage Basket",
                "Decorative Vase",
                "Area Rug",
                "Picture Frame Set",
                "Kitchen Utensil Set",
                "Bedding Set",
                "Curtains",
                "Floor Mat",
                "Watering Can"
            }) + $" - {f.Commerce.ProductMaterial()}",

            "Sports" => f.PickRandom(new[]
            {
                "Yoga Mat",
                "Dumbbells Set",
                "Tennis Racket",
                "Basketball",
                "Jump Rope",
                "Resistance Bands",
                "Water Bottle",
                "Exercise Ball",
                "Foam Roller",
                "Weight Lifting Gloves",
                "Running Belt",
                "Gym Bag",
                "Soccer Ball",
                "Badminton Set",
                "Bicycle Helmet"
            }) + $" {f.Commerce.ProductAdjective()}",

            "Toys" => f.PickRandom(new[]
            {
                "Building Blocks Set",
                "Remote Control Car",
                "Board Game",
                "Puzzle 1000 Pieces",
                "Action Figure",
                "Stuffed Animal",
                "Educational Toy",
                "Craft Kit",
                "Model Kit",
                "Playing Cards",
                "Toy Train Set",
                "Doll House",
                "Science Experiment Kit",
                "Art Supplies Set",
                "Musical Instrument Toy"
            }),

            "Food" => f.PickRandom(new[]
            {
                "Organic Coffee Beans",
                "Gourmet Chocolate Box",
                "Premium Tea Collection",
                "Protein Bars Box",
                "Nuts & Seeds Mix",
                "Olive Oil Premium",
                "Honey Pure Natural",
                "Spice Collection Set",
                "Pasta Premium",
                "Granola Bars",
                "Energy Drink Pack",
                "Snack Variety Pack",
                "Breakfast Cereal",
                "Dried Fruits Mix",
                "Cooking Sauce Set"
            }) + $" {f.Random.Number(100, 1000)}g",

            "Health & Beauty" => f.PickRandom(new[]
            {
                "Face Moisturizer",
                "Shampoo & Conditioner Set",
                "Body Lotion",
                "Vitamin Supplements",
                "Sunscreen SPF 50",
                "Makeup Kit",
                "Hair Dryer",
                "Electric Toothbrush",
                "Nail Care Set",
                "Facial Cleanser",
                "Body Wash",
                "Hand Cream",
                "Face Mask Set",
                "Essential Oils Kit",
                "Perfume"
            }) + $" - {f.Commerce.ProductAdjective()}",

            _ => f.Commerce.ProductName()
        };
    }

    private string GenerateProductDescription(Faker f, string productName, string category)
    {
        var features = new List<string>();
        
        for (int i = 0; i < f.Random.Number(3, 5); i++)
        {
            features.Add(f.Commerce.ProductAdjective() + " " + f.Commerce.ProductMaterial());
        }

        return $"{productName} - A premium {category.ToLower()} product featuring {string.Join(", ", features)}. " +
               $"{f.Lorem.Sentence(10)} " +
               $"Perfect for everyday use. {f.Lorem.Sentence(8)}";
    }

    private decimal GeneratePrice(Faker f, string category)
    {
        return category switch
        {
            "Electronics" => f.Finance.Amount(29.99m, 999.99m),
            "Clothing" => f.Finance.Amount(19.99m, 199.99m),
            "Books" => f.Finance.Amount(9.99m, 49.99m),
            "Home & Garden" => f.Finance.Amount(14.99m, 299.99m),
            "Sports" => f.Finance.Amount(19.99m, 249.99m),
            "Toys" => f.Finance.Amount(9.99m, 99.99m),
            "Food" => f.Finance.Amount(4.99m, 49.99m),
            "Health & Beauty" => f.Finance.Amount(9.99m, 149.99m),
            _ => f.Finance.Amount(9.99m, 299.99m)
        };
    }

    private string GenerateImageUrl(Faker f, string category)
    {
        // Using Unsplash Source for random category-based images
        // Alternative: Use Pixabay, Pexels, or iStock if you have API keys
        
        var categoryKeyword = category.ToLower().Replace(" & ", "-").Replace(" ", "-");
        
        // Unsplash Source provides random images based on keywords
        // Format: https://source.unsplash.com/800x600/?keyword
        var imageServices = new[]
        {
            $"https://source.unsplash.com/800x600/?{categoryKeyword},product",
            $"https://images.unsplash.com/photo-{f.Random.Long(1500000000000L, 1700000000000L)}?w=800&h=600&fit=crop",
        };

        // For production, you might want to use specific image URLs
        // Here are some category-specific placeholder patterns:
        return category switch
        {
            "Electronics" => f.PickRandom(new[]
            {
                "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800&h=600&fit=crop", // Headphones
                "https://images.unsplash.com/photo-1498049794561-7780e7231661?w=800&h=600&fit=crop", // Laptop
                "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=800&h=600&fit=crop", // Watch
                "https://images.unsplash.com/photo-1546868871-7041f2a55e12?w=800&h=600&fit=crop", // Smart Watch
                "https://images.unsplash.com/photo-1583394838336-acd977736f90?w=800&h=600&fit=crop", // Headphones
            }),
            
            "Clothing" => f.PickRandom(new[]
            {
                "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=800&h=600&fit=crop", // T-Shirt
                "https://images.unsplash.com/photo-1542272604-787c3835535d?w=800&h=600&fit=crop", // Sneakers
                "https://images.unsplash.com/photo-1556821840-3a63f95609a7?w=800&h=600&fit=crop", // Jeans
                "https://images.unsplash.com/photo-1591047139829-d91aecb6caea?w=800&h=600&fit=crop", // Jacket
                "https://images.unsplash.com/photo-1503342394128-c104d54dba01?w=800&h=600&fit=crop", // Shoes
            }),
            
            "Books" => f.PickRandom(new[]
            {
                "https://images.unsplash.com/photo-1512820790803-83ca734da794?w=800&h=600&fit=crop", // Books
                "https://images.unsplash.com/photo-1544947950-fa07a98d237f?w=800&h=600&fit=crop", // Books Stack
                "https://images.unsplash.com/photo-1495446815901-a7297e633e8d?w=800&h=600&fit=crop", // Open Book
                "https://images.unsplash.com/photo-1524578271613-d550eacf6090?w=800&h=600&fit=crop", // Library
                "https://images.unsplash.com/photo-1507842217343-583bb7270b66?w=800&h=600&fit=crop", // Book
            }),
            
            "Home & Garden" => f.PickRandom(new[]
            {
                "https://images.unsplash.com/photo-1493663284031-b7e3aefcae8e?w=800&h=600&fit=crop", // Home
                "https://images.unsplash.com/photo-1556228578-0d85b1a4d571?w=800&h=600&fit=crop", // Garden
                "https://images.unsplash.com/photo-1484101403633-562f891dc89a?w=800&h=600&fit=crop", // Plant
                "https://images.unsplash.com/photo-1513694203232-719a280e022f?w=800&h=600&fit=crop", // Interior
                "https://images.unsplash.com/photo-1615873968403-89e068629265?w=800&h=600&fit=crop", // Decor
            }),
            
            "Sports" => f.PickRandom(new[]
            {
                "https://images.unsplash.com/photo-1461896836934-ffe607ba8211?w=800&h=600&fit=crop", // Sports
                "https://images.unsplash.com/photo-1517836357463-d25dfeac3438?w=800&h=600&fit=crop", // Gym
                "https://images.unsplash.com/photo-1558611848-73f7eb4001a1?w=800&h=600&fit=crop", // Yoga
                "https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b?w=800&h=600&fit=crop", // Fitness
                "https://images.unsplash.com/photo-1434494343833-76b479733705?w=800&h=600&fit=crop", // Basketball
            }),
            
            "Toys" => f.PickRandom(new[]
            {
                "https://images.unsplash.com/photo-1558060370-d644479cb6f7?w=800&h=600&fit=crop", // Toys
                "https://images.unsplash.com/photo-1519340241574-2cec6aef0c01?w=800&h=600&fit=crop", // Lego
                "https://images.unsplash.com/photo-1587653915936-5623ea0b949a?w=800&h=600&fit=crop", // Toy Car
                "https://images.unsplash.com/photo-1515488042361-ee00e0ddd4e4?w=800&h=600&fit=crop", // Bear
                "https://images.unsplash.com/photo-1566576721346-d4a3b4eaeb55?w=800&h=600&fit=crop", // Game
            }),
            
            "Food" => f.PickRandom(new[]
            {
                "https://images.unsplash.com/photo-1506619216599-9d16d0903dfd?w=800&h=600&fit=crop", // Coffee
                "https://images.unsplash.com/photo-1511381939415-e44015466834?w=800&h=600&fit=crop", // Tea
                "https://images.unsplash.com/photo-1606312619070-d48b4ceb6bf1?w=800&h=600&fit=crop", // Chocolate
                "https://images.unsplash.com/photo-1587049352846-4a222e784343?w=800&h=600&fit=crop", // Snacks
                "https://images.unsplash.com/photo-1579372786545-d24232daf58c?w=800&h=600&fit=crop", // Nuts
            }),
            
            "Health & Beauty" => f.PickRandom(new[]
            {
                "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=600&fit=crop", // Beauty
                "https://images.unsplash.com/photo-1608248543803-ba4f8c70ae0b?w=800&h=600&fit=crop", // Skincare
                "https://images.unsplash.com/photo-1571781926291-c477ebfd024b?w=800&h=600&fit=crop", // Cosmetics
                "https://images.unsplash.com/photo-1570554886111-e80fcca6a029?w=800&h=600&fit=crop", // Perfume
                "https://images.unsplash.com/photo-1596755389378-c31d21fd1273?w=800&h=600&fit=crop", // Soap
            }),
            
            _ => imageServices[0]
        };
    }
}

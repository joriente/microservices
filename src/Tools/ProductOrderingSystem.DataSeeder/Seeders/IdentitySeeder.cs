using Bogus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using ProductOrderingSystem.DataSeeder.Infrastructure;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.DataSeeder.Seeders;

public class IdentitySeeder
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentitySeeder> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEventPublisher _eventPublisher;

    public IdentitySeeder(
        IConfiguration configuration,
        ILogger<IdentitySeeder> logger,
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
        var createAdmin = _configuration.GetValue<bool>("Seeding:Services:IdentityService:CreateAdminUser", true);
        var createShopper = _configuration.GetValue<bool>("Seeding:Services:IdentityService:CreateShopperUser", true);
        var publishEvents = _configuration.GetValue<bool>("Seeding:Services:IdentityService:PublishEvents", true);
        var clearExisting = _configuration.GetValue<bool>("Seeding:ClearExistingData", false);

        _logger.LogInformation("Identity seeding configuration:");
        _logger.LogInformation("  - Create admin: {CreateAdmin}", createAdmin);
        _logger.LogInformation("  - Create shopper: {CreateShopper}", createShopper);
        _logger.LogInformation("  - Publish events: {PublishEvents}", publishEvents);
        _logger.LogInformation("  - Clear existing: {Clear}", clearExisting);

        // Connect directly to MongoDB
        var mongoConnectionString = _configuration.GetConnectionString("MongoDB");
        var databaseName = _configuration.GetValue<string>("MongoDB:IdentityDatabaseName", "identitydb");
        var mongoClient = new MongoClient(mongoConnectionString);
        var database = mongoClient.GetDatabase(databaseName);
        var collection = database.GetCollection<UserDocument>("users");

        // Check existing count
        var existingCount = await collection.CountDocumentsAsync(FilterDefinition<UserDocument>.Empty);
        _logger.LogInformation("Current user count: {Count}", existingCount);

        if (clearExisting && existingCount > 0)
        {
            _logger.LogInformation("Clearing {Count} existing users...", existingCount);
            await collection.DeleteManyAsync(FilterDefinition<UserDocument>.Empty);
            _logger.LogInformation("✓ Cleared existing users");
        }

        var publishedEvents = 0;

        // Create admin user
        if (createAdmin)
        {
            var existingAdmin = await collection.Find(u => u.Username == "admin").FirstOrDefaultAsync();
            if (existingAdmin == null)
            {
                _logger.LogInformation("Creating admin user...");
                var adminUser = new UserDocument
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = "admin@productordering.com",
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ssw0rd"),
                    FirstName = "System",
                    LastName = "Administrator",
                    Roles = new List<string> { "Admin" },
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await collection.InsertOneAsync(adminUser);
                _logger.LogInformation("✓ Created admin user - Username: admin, Password: P@ssw0rd");

                // Publish UserCreatedEvent if enabled
                if (publishEvents)
                {
                    var @event = new UserCreatedEvent(
                        UserId: adminUser.Id,
                        Email: adminUser.Email,
                        FirstName: adminUser.FirstName,
                        LastName: adminUser.LastName,
                        CreatedAt: adminUser.CreatedAt
                    );

                    await _eventPublisher.PublishAsync(@event);
                    publishedEvents++;
                }
            }
            else
            {
                _logger.LogInformation("Admin user already exists, skipping creation.");
            }
        }

        // Create shopper user
        if (createShopper)
        {
            var existingShopper = await collection.Find(u => u.Username == "shopper").FirstOrDefaultAsync();
            if (existingShopper == null)
            {
                _logger.LogInformation("Creating shopper user...");
                var shopperUser = new UserDocument
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = "steve.hopper@email.com",
                    Username = "shopper",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ssw0rd"),
                    FirstName = "Steve",
                    LastName = "Hopper",
                    Roles = new List<string>(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await collection.InsertOneAsync(shopperUser);
                _logger.LogInformation("✓ Created shopper user - Username: shopper, Password: P@ssw0rd");

                // Publish UserCreatedEvent if enabled
                if (publishEvents)
                {
                    var @event = new UserCreatedEvent(
                        UserId: shopperUser.Id,
                        Email: shopperUser.Email,
                        FirstName: shopperUser.FirstName,
                        LastName: shopperUser.LastName,
                        CreatedAt: shopperUser.CreatedAt
                    );

                    await _eventPublisher.PublishAsync(@event);
                    publishedEvents++;
                }
            }
            else
            {
                _logger.LogInformation("Shopper user already exists, skipping creation.");
            }
        }

        if (publishEvents && publishedEvents > 0)
        {
            _logger.LogInformation("✓ Published {Count} UserCreatedEvent messages", publishedEvents);
        }
    }
}

// MongoDB document model for direct insertion
public class UserDocument
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

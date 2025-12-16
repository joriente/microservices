using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.DataSeeder.Infrastructure;

namespace ProductOrderingSystem.DataSeeder.Seeders;

public class DataSeederRunner
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DataSeederRunner> _logger;
    private readonly ProductSeeder _productSeeder;
    private readonly IdentitySeeder _identitySeeder;
    private readonly IRabbitMqReadinessChecker _rabbitMqChecker;

    public DataSeederRunner(
        IConfiguration configuration,
        ILogger<DataSeederRunner> logger,
        ProductSeeder productSeeder,
        IdentitySeeder identitySeeder,
        IRabbitMqReadinessChecker rabbitMqChecker)
    {
        _configuration = configuration;
        _logger = logger;
        _productSeeder = productSeeder;
        _identitySeeder = identitySeeder;
        _rabbitMqChecker = rabbitMqChecker;
    }

    public async Task RunAsync()
    {
        var seedingEnabled = _configuration.GetValue<bool>("Seeding:Enabled");
        if (!seedingEnabled)
        {
            _logger.LogWarning("Seeding is disabled in configuration. Exiting.");
            return;
        }

        _logger.LogInformation("Seeding configuration loaded:");
        _logger.LogInformation("  - Clear existing data: {Clear}", 
            _configuration.GetValue<bool>("Seeding:ClearExistingData"));

        // Wait for consumer services to fully initialize their MassTransit consumers
        // Check RabbitMQ to verify consumer queues exist and have active consumers
        var expectedQueues = new[] { "ProductCreatedEvent", "ProductUpdatedEvent", "ProductDeletedEvent" };
        
        var maxWaitSeconds = _configuration.GetValue<int>("Seeding:MaxConsumerWaitSeconds", 60);
        
        _logger.LogInformation("");
        _logger.LogInformation("========================================");
        _logger.LogInformation("⏳ Waiting for consumer services to register queues in RabbitMQ...");
        _logger.LogInformation("   Checking for: {Queues}", string.Join(", ", expectedQueues));
        _logger.LogInformation("   Max wait time: {Seconds} seconds", maxWaitSeconds);
        _logger.LogInformation("========================================");
        
        var isReady = await _rabbitMqChecker.WaitForQueuesAsync(expectedQueues, TimeSpan.FromSeconds(maxWaitSeconds));
        
        if (!isReady)
        {
            _logger.LogWarning("⚠️  Timeout waiting for consumer queues. Proceeding anyway...");
            _logger.LogWarning("   Product cache updates may not be received by consumer services.");
        }
        
        // Purge old messages from queues to prevent stale data
        var clearExisting = _configuration.GetValue<bool>("Seeding:ClearExistingData");
        if (clearExisting)
        {
            _logger.LogInformation("");
            _logger.LogInformation("Purging old messages from RabbitMQ queues...");
            await _rabbitMqChecker.PurgeQueuesAsync(expectedQueues);
        }
        
        _logger.LogInformation("");

        // Seed Identity Service
        if (_configuration.GetValue<bool>("Seeding:Services:IdentityService:Enabled"))
        {
            _logger.LogInformation("");
            _logger.LogInformation("=== Seeding Identity Service ===");
            try
            {
                await _identitySeeder.SeedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Identity seeding failed");
            }
        }

        // Seed Product Service
        if (_configuration.GetValue<bool>("Seeding:Services:ProductService:Enabled"))
        {
            _logger.LogInformation("");
            _logger.LogInformation("=== Seeding Product Service ===");
            try
            {
                await _productSeeder.SeedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Product seeding failed");
            }
        }

        // Resync product cache by republishing events for all existing products
        // This ensures consumers that registered after initial seeding receive the events
        var resyncEnabled = _configuration.GetValue<bool>("Seeding:Services:ProductService:ResyncCache", true);
        if (resyncEnabled && _configuration.GetValue<bool>("Seeding:Services:ProductService:Enabled"))
        {
            _logger.LogInformation("");
            _logger.LogInformation("=== Resyncing Product Cache ===");
            _logger.LogInformation("Republishing ProductCreatedEvent for all existing products...");
            try
            {
                await _productSeeder.ResyncCacheAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Product cache resync failed");
            }
        }

        _logger.LogInformation("");
        _logger.LogInformation("All seeding operations completed!");
    }
}

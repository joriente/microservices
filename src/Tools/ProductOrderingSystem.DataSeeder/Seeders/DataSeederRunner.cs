using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ProductOrderingSystem.DataSeeder.Seeders;

public class DataSeederRunner
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DataSeederRunner> _logger;
    private readonly ProductSeeder _productSeeder;
    private readonly IdentitySeeder _identitySeeder;

    public DataSeederRunner(
        IConfiguration configuration,
        ILogger<DataSeederRunner> logger,
        ProductSeeder productSeeder,
        IdentitySeeder identitySeeder)
    {
        _configuration = configuration;
        _logger = logger;
        _productSeeder = productSeeder;
        _identitySeeder = identitySeeder;
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

        _logger.LogInformation("");
        _logger.LogInformation("All seeding operations completed!");
    }
}

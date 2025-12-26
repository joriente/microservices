using Wolverine;
using ProductOrderingSystem.IdentityService.Application.Commands.Auth;
using ProductOrderingSystem.IdentityService.Domain.Repositories;
using ProductOrderingSystem.Shared.Contracts.Identity;
using ErrorOr;

namespace ProductOrderingSystem.IdentityService.WebAPI.Data;

public class AdminUserSeeder : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AdminUserSeeder> _logger;

    public AdminUserSeeder(IServiceProvider serviceProvider, ILogger<AdminUserSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for infrastructure to be ready
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        var maxRetries = 20;
        var retryDelay = TimeSpan.FromSeconds(5);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("AdminUserSeeder: Attempt {Attempt}/{MaxRetries} to seed admin user...", attempt, maxRetries);

                using var scope = _serviceProvider.CreateScope();
                var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

                // Check if admin user already exists
                var existingAdmin = await userRepository.GetByUsernameAsync("admin", stoppingToken);
                if (existingAdmin == null)
                {
                    // Create admin user using message bus to ensure events are published
                    var adminCommand = new RegisterUserCommand(
                        Email: "admin@productordering.com",
                        Username: "admin",
                        Password: "P@ssw0rd",
                        FirstName: "System",
                        LastName: "Administrator"
                    );

                    var adminResult = await messageBus.InvokeAsync<ErrorOr.ErrorOr<ProductOrderingSystem.Shared.Contracts.Identity.UserDto>>(adminCommand, stoppingToken);

                    if (adminResult.IsError)
                    {
                        _logger.LogWarning("AdminUserSeeder: Failed to create admin user - {Errors}", 
                            string.Join(", ", adminResult.Errors.Select(e => e.Description)));
                        
                        if (attempt < maxRetries)
                        {
                            _logger.LogInformation("AdminUserSeeder: Retrying in {Delay} seconds...", retryDelay.TotalSeconds);
                            await Task.Delay(retryDelay, stoppingToken);
                            continue;
                        }
                        return;
                    }

                    var adminUser = adminResult.Value;

                    // Add Admin role directly to the user entity
                    var user = await userRepository.GetByIdAsync(adminUser.Id, stoppingToken);
                    if (user != null)
                    {
                        user.AddRole("Admin");
                        await userRepository.UpdateAsync(user, stoppingToken);
                        
                        _logger.LogInformation("AdminUserSeeder: Successfully created admin user with ID: {UserId}", adminUser.Id);
                        _logger.LogInformation("AdminUserSeeder: Admin credentials - Username: admin, Password: P@ssw0rd");
                    }
                }
                else
                {
                    _logger.LogInformation("AdminUserSeeder: Admin user already exists, skipping admin creation.");
                }

                // Check if shopper user Steve Hopper already exists
                var existingShopper = await userRepository.GetByUsernameAsync("steve.hopper", stoppingToken);
                if (existingShopper == null)
                {
                    // Create shopper user Steve Hopper
                    var shopperCommand = new RegisterUserCommand(
                        Email: "steve.hopper@email.com",
                        Username: "shopper",
                        Password: "P@ssw0rd",
                        FirstName: "Steve",
                        LastName: "Hopper"
                    );

                    var shopperResult = await messageBus.InvokeAsync<ErrorOr<UserDto>>(shopperCommand);

                    if (shopperResult.IsError)
                    {
                        _logger.LogWarning("AdminUserSeeder: Failed to create shopper user - {Errors}", 
                            string.Join(", ", shopperResult.Errors.Select(e => e.Description)));
                    }
                    else
                    {
                        _logger.LogInformation("AdminUserSeeder: Successfully created shopper user with ID: {UserId}", shopperResult.Value.Id);
                        _logger.LogInformation("AdminUserSeeder: Shopper credentials - Username: steve.hopper, Password: P@ssw0rd");
                    }
                }
                else
                {
                    _logger.LogInformation("AdminUserSeeder: Shopper user already exists, skipping shopper creation.");
                }

                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AdminUserSeeder: Error on attempt {Attempt}/{MaxRetries}", attempt, maxRetries);

                if (attempt < maxRetries)
                {
                    _logger.LogInformation("AdminUserSeeder: Retrying in {Delay} seconds...", retryDelay.TotalSeconds);
                    await Task.Delay(retryDelay, stoppingToken);
                }
                else
                {
                    _logger.LogError("AdminUserSeeder: Failed to seed admin user after {MaxRetries} attempts", maxRetries);
                }
            }
        }
    }
}

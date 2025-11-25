using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Testcontainers.RabbitMq;

namespace ProductOrderingSystem.CustomerService.IntegrationTests;

public class CustomerServiceWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer = new MongoDbBuilder()
        .WithImage("mongo:8.0")
        .WithUsername("admin")
        .WithPassword("admin123")
        .Build();

    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:4.1-management")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    public const string SecretKey = "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
    public const string Issuer = "ProductOrderingSystem";
    public const string Audience = "ProductOrderingSystem.Services";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove existing MongoDB registration
            services.RemoveAll<IMongoDatabase>();
            services.RemoveAll<IMongoClient>();

            // Register test MongoDB
            services.AddSingleton<IMongoClient>(sp =>
            {
                return new MongoClient(_mongoContainer.GetConnectionString());
            });

            services.AddSingleton<IMongoDatabase>(sp =>
            {
                var client = sp.GetRequiredService<IMongoClient>();
                return client.GetDatabase("customerdb-test");
            });

            // Reconfigure JWT Bearer authentication with test settings
            services.PostConfigureAll<JwtBearerOptions>(options =>
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = Issuer,
                    ValidateAudience = true,
                    ValidAudience = Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            // Override RabbitMQ connection
            builder.UseSetting("RabbitMQ:Host", _rabbitMqContainer.Hostname);
            builder.UseSetting("RabbitMQ:Port", _rabbitMqContainer.GetMappedPublicPort(5672).ToString());
        });

        builder.UseEnvironment("Development");
    }

    public async Task InitializeAsync()
    {
        await _mongoContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _mongoContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}

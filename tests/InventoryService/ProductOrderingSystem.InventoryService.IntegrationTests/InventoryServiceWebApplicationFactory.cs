using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Testcontainers.RabbitMq;

namespace ProductOrderingSystem.InventoryService.IntegrationTests;

public class InventoryServiceWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer = new MongoDbBuilder()
        .WithImage("mongo:8.0")
        .WithUsername("admin")
        .WithPassword("admin123")
        .Build();

    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:4.1-management")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove the existing MongoDB registration
            services.RemoveAll<IMongoClient>();
            services.RemoveAll<IMongoDatabase>();

            // Add test MongoDB
            var mongoClient = new MongoClient(_mongoContainer.GetConnectionString());
            var database = mongoClient.GetDatabase("inventorydb-test");

            services.AddSingleton<IMongoClient>(mongoClient);
            services.AddSingleton(database);

            // Configure test RabbitMQ connection
            Environment.SetEnvironmentVariable("RABBITMQ_HOST", _rabbitMqContainer.Hostname);
            Environment.SetEnvironmentVariable("RABBITMQ_PORT", _rabbitMqContainer.GetMappedPublicPort(5672).ToString());
        });
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
    }
}

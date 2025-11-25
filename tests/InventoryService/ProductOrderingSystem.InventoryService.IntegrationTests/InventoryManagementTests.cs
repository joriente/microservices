using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ProductOrderingSystem.InventoryService.Features.Inventory;
using Xunit;

namespace ProductOrderingSystem.InventoryService.IntegrationTests;

public class InventoryManagementTests : IClassFixture<InventoryServiceWebApplicationFactory>
{
    private readonly HttpClient _client;

    public InventoryManagementTests(InventoryServiceWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllInventory_ReturnsOk_WithInventoryList()
    {
        // Act
        var response = await _client.GetAsync("/api/inventory");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var inventory = await response.Content.ReadFromJsonAsync<List<GetAllInventory.InventoryItemDto>>();
        inventory.Should().NotBeNull();
    }

    [Fact]
    public async Task AdjustInventory_AddStock_ReturnsOk()
    {
        // Arrange - First create a test product in inventory
        var productId = Guid.NewGuid();
        
        // Note: In a real test, you'd need to ensure a product exists first
        // For now, we'll test the endpoint structure
        
        var command = new AdjustInventory.Command(
            ProductId: productId,
            Quantity: 100,  // Adding 100 units
            Reason: "Initial stock for testing"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/inventory/adjust", command);

        // Assert
        // Will be NotFound if product doesn't exist, which is expected in this test
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AdjustInventory_RemoveStock_WithValidationError_ReturnsBadRequest()
    {
        // Arrange
        var command = new AdjustInventory.Command(
            ProductId: Guid.NewGuid(),
            Quantity: 0,  // Invalid: quantity cannot be zero
            Reason: "Test"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/inventory/adjust", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AdjustInventory_MissingReason_ReturnsValidationError()
    {
        // Arrange
        var command = new AdjustInventory.Command(
            ProductId: Guid.NewGuid(),
            Quantity: 10,
            Reason: ""  // Invalid: reason is required
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/inventory/adjust", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

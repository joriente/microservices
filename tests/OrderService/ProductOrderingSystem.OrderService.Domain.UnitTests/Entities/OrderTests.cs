using AwesomeAssertions;
using ProductOrderingSystem.OrderService.Domain.Entities;

namespace ProductOrderingSystem.OrderService.Domain.UnitTests.Entities;

public class OrderTests
{
    [Fact]
    public void Constructor_ShouldCreateOrder_WithCorrectInitialValues()
    {
        // Arrange
        var customerId = "cust-123";
        var customerEmail = "test@example.com";
        var customerName = "John Doe";
        var items = new List<OrderItem>
        {
            new("prod-1", "Product 1", 10.0m, 2)
        };
        var notes = "Test notes";

        // Act
        var order = new Order(customerId, customerEmail, customerName, items, notes);

        // Assert
        order.Id.Should().NotBeNullOrEmpty();
        order.CustomerId.Should().Be(customerId);
        order.CustomerEmail.Should().Be(customerEmail);
        order.CustomerName.Should().Be(customerName);
        order.Items.Should().HaveCount(1);
        order.Notes.Should().Be(notes);
        order.Status.Should().Be(OrderStatus.Pending);
        order.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        order.UpdatedAt.Should().BeNull();
        order.TotalAmount.Should().Be(20.0m); // 10.0 * 2
    }

    [Fact]
    public void Constructor_ShouldHandleNullItemsList()
    {
        // Arrange & Act
        var order = new Order("cust-1", "test@example.com", "John", null!);

        // Assert
        order.Items.Should().NotBeNull();
        order.Items.Should().BeEmpty();
        order.TotalAmount.Should().Be(0);
    }

    [Fact]
    public void CalculateTotal_ShouldSumAllItemTotalPrices()
    {
        // Arrange
        var order = new Order();
        order.Items = new List<OrderItem>
        {
            new("prod-1", "Product 1", 10.0m, 2), // 20.0
            new("prod-2", "Product 2", 15.0m, 3), // 45.0
            new("prod-3", "Product 3", 5.0m, 1)   // 5.0
        };

        // Act
        order.CalculateTotal();

        // Assert
        order.TotalAmount.Should().Be(70.0m); // 20 + 45 + 5
    }

    [Fact]
    public void AddItem_ShouldAddNewItem_WhenProductDoesNotExist()
    {
        // Arrange
        var order = new Order("cust-1", "test@example.com", "John", new List<OrderItem>());

        // Act
        order.AddItem("prod-1", "Product 1", 10.0m, 2);

        // Assert
        order.Items.Should().ContainSingle();
        order.Items[0].ProductId.Should().Be("prod-1");
        order.Items[0].ProductName.Should().Be("Product 1");
        order.Items[0].Quantity.Should().Be(2);
        order.TotalAmount.Should().Be(20.0m);
        order.UpdatedAt.Should().NotBeNull();
        order.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AddItem_ShouldIncreaseQuantity_WhenProductAlreadyExists()
    {
        // Arrange
        var order = new Order();
        order.Items = new List<OrderItem>
        {
            new("prod-1", "Product 1", 10.0m, 2)
        };
        order.CalculateTotal();

        // Act
        order.AddItem("prod-1", "Product 1", 10.0m, 3);

        // Assert
        order.Items.Should().ContainSingle();
        order.Items[0].Quantity.Should().Be(5); // 2 + 3
        order.TotalAmount.Should().Be(50.0m); // 10 * 5
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void AddItem_ShouldThrowException_WhenQuantityIsZeroOrNegative(int invalidQuantity)
    {
        // Arrange
        var order = new Order();

        // Act
        var act = () => order.AddItem("prod-1", "Product 1", 10.0m, invalidQuantity);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Quantity must be greater than zero*");
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(-10.50)]
    public void AddItem_ShouldThrowException_WhenUnitPriceIsNegative(decimal invalidPrice)
    {
        // Arrange
        var order = new Order();

        // Act
        var act = () => order.AddItem("prod-1", "Product 1", invalidPrice, 1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Unit price cannot be negative*");
    }

    [Fact]
    public void RemoveItem_ShouldRemoveItem_WhenProductExists()
    {
        // Arrange
        var order = new Order();
        order.Items = new List<OrderItem>
        {
            new("prod-1", "Product 1", 10.0m, 2),
            new("prod-2", "Product 2", 15.0m, 1)
        };
        order.CalculateTotal();
        var initialTotal = order.TotalAmount;

        // Act
        order.RemoveItem("prod-1");

        // Assert
        order.Items.Should().ContainSingle();
        order.Items.Should().NotContain(i => i.ProductId == "prod-1");
        order.TotalAmount.Should().Be(15.0m);
        order.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void RemoveItem_ShouldDoNothing_WhenProductDoesNotExist()
    {
        // Arrange
        var order = new Order();
        order.Items = new List<OrderItem>
        {
            new("prod-1", "Product 1", 10.0m, 2)
        };
        order.CalculateTotal();

        // Act
        order.RemoveItem("nonexistent-product");

        // Assert
        order.Items.Should().ContainSingle();
        order.Items[0].ProductId.Should().Be("prod-1");
    }

    [Fact]
    public void UpdateStatus_ShouldChangeStatus_AndSetUpdatedAt()
    {
        // Arrange
        var order = new Order();
        var initialStatus = order.Status;

        // Act
        order.UpdateStatus(OrderStatus.Confirmed);

        // Assert
        order.Status.Should().Be(OrderStatus.Confirmed);
        order.Status.Should().NotBe(initialStatus);
        order.UpdatedAt.Should().NotBeNull();
        order.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(OrderStatus.Pending, true)]
    [InlineData(OrderStatus.Confirmed, true)]
    [InlineData(OrderStatus.Shipped, false)]
    [InlineData(OrderStatus.Delivered, false)]
    [InlineData(OrderStatus.Cancelled, false)]
    public void CanBeCancelled_ShouldReturnCorrectValue_BasedOnStatus(OrderStatus status, bool expectedResult)
    {
        // Arrange
        var order = new Order();
        order.UpdateStatus(status);

        // Act
        var result = order.CanBeCancelled();

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void Cancel_ShouldCancelOrder_WhenStatusIsPending()
    {
        // Arrange
        var order = new Order();
        order.UpdateStatus(OrderStatus.Pending);
        var reason = "Customer changed mind";

        // Act
        order.Cancel(reason);

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancellationReason.Should().Be(reason);
        order.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_ShouldCancelOrder_WhenStatusIsConfirmed()
    {
        // Arrange
        var order = new Order();
        order.UpdateStatus(OrderStatus.Confirmed);
        var reason = "Out of stock";

        // Act
        order.Cancel(reason);

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancellationReason.Should().Be(reason);
    }

    [Theory]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public void Cancel_ShouldThrowException_WhenStatusDoesNotAllowCancellation(OrderStatus status)
    {
        // Arrange
        var order = new Order();
        order.UpdateStatus(status);

        // Act
        var act = () => order.Cancel("Some reason");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Order cannot be cancelled when status is {status}");
    }

    [Fact]
    public void Cancel_WithoutReason_ShouldUseDefaultReason()
    {
        // Arrange
        var order = new Order();
        order.UpdateStatus(OrderStatus.Pending);

        // Act
        order.Cancel();

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancellationReason.Should().Be("Order cancelled");
    }
}

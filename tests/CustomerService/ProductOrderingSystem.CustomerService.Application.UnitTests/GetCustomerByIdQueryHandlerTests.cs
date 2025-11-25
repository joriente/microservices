using AwesomeAssertions;
using ErrorOr;
using Moq;
using ProductOrderingSystem.CustomerService.Application.Customers.Commands.CreateCustomer;
using ProductOrderingSystem.CustomerService.Application.Customers.Queries.GetCustomerById;
using ProductOrderingSystem.CustomerService.Domain.Entities;
using ProductOrderingSystem.CustomerService.Domain.Repositories;

namespace ProductOrderingSystem.CustomerService.Application.UnitTests;

public class GetCustomerByIdQueryHandlerTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock;
    private readonly GetCustomerByIdQueryHandler _handler;

    public GetCustomerByIdQueryHandlerTests()
    {
        _repositoryMock = new Mock<ICustomerRepository>();
        _handler = new GetCustomerByIdQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnCustomer_WhenCustomerExists()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customer = Customer.Create(
            "test@example.com",
            "John",
            "Doe",
            "555-1234"
        );
        
        // Set the ID using reflection to match the query
        var idProperty = typeof(Customer).GetProperty(nameof(Customer.Id));
        idProperty?.SetValue(customer, customerId);

        var query = new GetCustomerByIdQuery(customerId);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(customerId);
        result.Value.Email.Should().Be("test@example.com");
        result.Value.FirstName.Should().Be("John");
        result.Value.LastName.Should().Be("Doe");
        result.Value.PhoneNumber.Should().Be("555-1234");

        _repositoryMock.Verify(
            r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFoundError_WhenCustomerDoesNotExist()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var query = new GetCustomerByIdQuery(customerId);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        result.FirstError.Code.Should().Be("Customer.NotFound");
        result.FirstError.Description.Should().Contain(customerId.ToString());

        _repositoryMock.Verify(
            r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnCustomerWithAddresses()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customer = Customer.Create("test@example.com", "John", "Doe");
        
        var idProperty = typeof(Customer).GetProperty(nameof(Customer.Id));
        idProperty?.SetValue(customer, customerId);

        customer.AddAddress(new Domain.ValueObjects.Address
        {
            Street = "123 Main St",
            City = "Springfield",
            State = "IL",
            PostalCode = "62701",
            Country = "USA"
        });

        var query = new GetCustomerByIdQuery(customerId);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Addresses.Should().HaveCount(1);
        result.Value.Addresses.First().Street.Should().Be("123 Main St");
        result.Value.Addresses.First().City.Should().Be("Springfield");
        result.Value.Addresses.First().IsDefault.Should().BeTrue(); // First address is default
    }
}

using AwesomeAssertions;
using ErrorOr;
using Moq;
using ProductOrderingSystem.CustomerService.Application.Customers.Commands.CreateCustomer;
using ProductOrderingSystem.CustomerService.Domain.Entities;
using ProductOrderingSystem.CustomerService.Domain.Repositories;

namespace ProductOrderingSystem.CustomerService.Application.UnitTests;

public class CreateCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock;
    private readonly CreateCustomerCommandHandler _handler;

    public CreateCustomerCommandHandlerTests()
    {
        _repositoryMock = new Mock<ICustomerRepository>();
        _handler = new CreateCustomerCommandHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_CreateCustomer_WhenValidCommand()
    {
        // Arrange
        var command = new CreateCustomerCommand(
            Email: "test@example.com",
            FirstName: "John",
            LastName: "Doe",
            PhoneNumber: "555-1234"
        );

        _repositoryMock
            .Setup(r => r.EmailExistsAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer c, CancellationToken ct) => c);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Email.Should().Be(command.Email.ToLowerInvariant());
        result.Value.FirstName.Should().Be(command.FirstName);
        result.Value.LastName.Should().Be(command.LastName);
        result.Value.PhoneNumber.Should().Be(command.PhoneNumber);

        _repositoryMock.Verify(
            r => r.EmailExistsAsync(command.Email, It.IsAny<CancellationToken>()),
            Times.Once);

        _repositoryMock.Verify(
            r => r.AddAsync(It.Is<Customer>(c => 
                c.Email == command.Email.ToLowerInvariant() &&
                c.FirstName == command.FirstName &&
                c.LastName == command.LastName &&
                c.PhoneNumber == command.PhoneNumber), 
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenEmailAlreadyExists()
    {
        // Arrange
        var command = new CreateCustomerCommand(
            Email: "existing@example.com",
            FirstName: "John",
            LastName: "Doe",
            PhoneNumber: null
        );

        _repositoryMock
            .Setup(r => r.EmailExistsAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Conflict);
        result.FirstError.Code.Should().Be("Customer.EmailExists");
        result.FirstError.Description.Should().Contain(command.Email);

        _repositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_CreateCustomer_WithoutPhoneNumber()
    {
        // Arrange
        var command = new CreateCustomerCommand(
            Email: "test@example.com",
            FirstName: "John",
            LastName: "Doe",
            PhoneNumber: null
        );

        _repositoryMock
            .Setup(r => r.EmailExistsAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer c, CancellationToken ct) => c);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.PhoneNumber.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_NormalizeEmailToLowercase()
    {
        // Arrange
        var command = new CreateCustomerCommand(
            Email: "TEST@EXAMPLE.COM",
            FirstName: "John",
            LastName: "Doe",
            PhoneNumber: null
        );

        _repositoryMock
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer c, CancellationToken ct) => c);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Email.Should().Be("test@example.com");
    }
}

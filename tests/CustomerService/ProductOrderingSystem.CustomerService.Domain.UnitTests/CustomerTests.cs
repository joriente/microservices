using AwesomeAssertions;
using ProductOrderingSystem.CustomerService.Domain.Entities;
using ProductOrderingSystem.CustomerService.Domain.Events;
using ProductOrderingSystem.CustomerService.Domain.ValueObjects;

namespace ProductOrderingSystem.CustomerService.Domain.UnitTests;

public class CustomerTests
{
    [Fact]
    public void Create_Should_CreateCustomerWithValidProperties()
    {
        // Arrange
        var email = "test@example.com";
        var firstName = "John";
        var lastName = "Doe";
        var phoneNumber = "555-1234";

        // Act
        var customer = Customer.Create(email, firstName, lastName, phoneNumber);

        // Assert
        customer.Should().NotBeNull();
        customer.Id.Should().NotBeEmpty();
        customer.Email.Should().Be(email.ToLowerInvariant());
        customer.FirstName.Should().Be(firstName);
        customer.LastName.Should().Be(lastName);
        customer.PhoneNumber.Should().Be(phoneNumber);
        customer.IsActive.Should().BeTrue();
        customer.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        customer.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        customer.Addresses.Should().BeEmpty();
    }

    [Fact]
    public void Create_Should_NormalizeEmailToLowercase()
    {
        // Arrange
        var email = "TEST@EXAMPLE.COM";

        // Act
        var customer = Customer.Create(email, "John", "Doe");

        // Assert
        customer.Email.Should().Be("test@example.com");
    }

    [Fact]
    public void Create_Should_AddCustomerCreatedDomainEvent()
    {
        // Arrange & Act
        var customer = Customer.Create("test@example.com", "John", "Doe");

        // Assert
        customer.DomainEvents.Should().HaveCount(1);
        customer.DomainEvents.First().Should().BeOfType<CustomerCreatedEvent>();
        
        var domainEvent = customer.DomainEvents.First() as CustomerCreatedEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.CustomerId.Should().Be(customer.Id);
        domainEvent.Email.Should().Be(customer.Email);
        domainEvent.FirstName.Should().Be(customer.FirstName);
        domainEvent.LastName.Should().Be(customer.LastName);
    }

    [Fact]
    public void UpdateDetails_Should_UpdateCustomerProperties()
    {
        // Arrange
        var customer = Customer.Create("test@example.com", "John", "Doe");
        var originalUpdatedAt = customer.UpdatedAt;
        Thread.Sleep(10); // Ensure time difference

        var newFirstName = "Jane";
        var newLastName = "Smith";
        var newPhone = "555-9999";

        // Act
        customer.UpdateDetails(newFirstName, newLastName, newPhone);

        // Assert
        customer.FirstName.Should().Be(newFirstName);
        customer.LastName.Should().Be(newLastName);
        customer.PhoneNumber.Should().Be(newPhone);
        customer.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdateDetails_Should_AddCustomerUpdatedDomainEvent()
    {
        // Arrange
        var customer = Customer.Create("test@example.com", "John", "Doe");
        customer.ClearDomainEvents(); // Clear creation event

        // Act
        customer.UpdateDetails("Jane", "Smith", "555-9999");

        // Assert
        customer.DomainEvents.Should().HaveCount(1);
        customer.DomainEvents.First().Should().BeOfType<CustomerUpdatedEvent>();
    }

    [Fact]
    public void AddAddress_Should_AddAddressToCustomer()
    {
        // Arrange
        var customer = Customer.Create("test@example.com", "John", "Doe");
        var address = new Address
        {
            Street = "123 Main St",
            City = "Springfield",
            State = "IL",
            PostalCode = "62701",
            Country = "USA",
            Type = AddressType.Shipping
        };

        // Act
        customer.AddAddress(address);

        // Assert
        customer.Addresses.Should().HaveCount(1);
        customer.Addresses.First().Should().Be(address);
    }

    [Fact]
    public void AddAddress_Should_SetFirstAddressAsDefault()
    {
        // Arrange
        var customer = Customer.Create("test@example.com", "John", "Doe");
        var address = new Address
        {
            Street = "123 Main St",
            City = "Springfield",
            State = "IL",
            PostalCode = "62701",
            Country = "USA"
        };

        // Act
        customer.AddAddress(address);

        // Assert
        address.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void AddAddress_Should_UnsetOtherDefaultAddresses_WhenAddingDefaultAddress()
    {
        // Arrange
        var customer = Customer.Create("test@example.com", "John", "Doe");
        
        var address1 = new Address
        {
            Street = "123 Main St",
            City = "Springfield",
            State = "IL",
            PostalCode = "62701",
            Country = "USA"
        };
        customer.AddAddress(address1);

        var address2 = new Address
        {
            Street = "456 Oak Ave",
            City = "Springfield",
            State = "IL",
            PostalCode = "62702",
            Country = "USA",
            IsDefault = true
        };

        // Act
        customer.AddAddress(address2);

        // Assert
        customer.Addresses.Should().HaveCount(2);
        address1.IsDefault.Should().BeFalse();
        address2.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void AddAddress_Should_AddAddressAddedDomainEvent()
    {
        // Arrange
        var customer = Customer.Create("test@example.com", "John", "Doe");
        customer.ClearDomainEvents();
        
        var address = new Address
        {
            Street = "123 Main St",
            City = "Springfield",
            State = "IL",
            PostalCode = "62701",
            Country = "USA"
        };

        // Act
        customer.AddAddress(address);

        // Assert
        customer.DomainEvents.Should().HaveCount(1);
        customer.DomainEvents.First().Should().BeOfType<AddressAddedEvent>();
    }

    [Fact]
    public void SetDefaultAddress_Should_SetAddressAsDefault()
    {
        // Arrange
        var customer = Customer.Create("test@example.com", "John", "Doe");
        
        var address1 = new Address
        {
            Street = "123 Main St",
            City = "Springfield",
            State = "IL",
            PostalCode = "62701",
            Country = "USA"
        };
        customer.AddAddress(address1);

        var address2 = new Address
        {
            Street = "456 Oak Ave",
            City = "Springfield",
            State = "IL",
            PostalCode = "62702",
            Country = "USA"
        };
        customer.AddAddress(address2);

        // Act
        customer.SetDefaultAddress(address2.Id);

        // Assert
        address1.IsDefault.Should().BeFalse();
        address2.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void SetDefaultAddress_Should_ThrowException_WhenAddressNotFound()
    {
        // Arrange
        var customer = Customer.Create("test@example.com", "John", "Doe");
        var nonExistentAddressId = Guid.NewGuid();

        // Act
        var act = () => customer.SetDefaultAddress(nonExistentAddressId);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Address not found");
    }

    [Fact]
    public void Deactivate_Should_SetIsActiveToFalse()
    {
        // Arrange
        var customer = Customer.Create("test@example.com", "John", "Doe");

        // Act
        customer.Deactivate();

        // Assert
        customer.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_Should_SetIsActiveToTrue()
    {
        // Arrange
        var customer = Customer.Create("test@example.com", "John", "Doe");
        customer.Deactivate();

        // Act
        customer.Activate();

        // Assert
        customer.IsActive.Should().BeTrue();
    }

    [Fact]
    public void GetFullName_Should_ReturnFormattedName()
    {
        // Arrange
        var customer = Customer.Create("test@example.com", "John", "Doe");

        // Act
        var fullName = customer.GetFullName();

        // Assert
        fullName.Should().Be("John Doe");
    }

    [Fact]
    public void ClearDomainEvents_Should_RemoveAllDomainEvents()
    {
        // Arrange
        var customer = Customer.Create("test@example.com", "John", "Doe");
        customer.DomainEvents.Should().NotBeEmpty();

        // Act
        customer.ClearDomainEvents();

        // Assert
        customer.DomainEvents.Should().BeEmpty();
    }
}

using AwesomeAssertions;
using ProductOrderingSystem.CustomerService.Domain.ValueObjects;

namespace ProductOrderingSystem.CustomerService.Domain.UnitTests;

public class AddressTests
{
    [Fact]
    public void Address_Should_HaveUniqueId_WhenCreated()
    {
        // Arrange & Act
        var address1 = new Address();
        var address2 = new Address();

        // Assert
        address1.Id.Should().NotBeEmpty();
        address2.Id.Should().NotBeEmpty();
        address1.Id.Should().NotBe(address2.Id);
    }

    [Fact]
    public void Address_Should_InitializeWithDefaultValues()
    {
        // Arrange & Act
        var address = new Address();

        // Assert
        address.Street.Should().Be(string.Empty);
        address.City.Should().Be(string.Empty);
        address.State.Should().Be(string.Empty);
        address.PostalCode.Should().Be(string.Empty);
        address.Country.Should().Be(string.Empty);
        address.IsDefault.Should().BeFalse();
        address.Type.Should().Be(AddressType.Shipping);
    }

    [Fact]
    public void Address_Should_AllowSettingAllProperties()
    {
        // Arrange
        var address = new Address();

        // Act
        address.Street = "123 Main St";
        address.City = "Springfield";
        address.State = "IL";
        address.PostalCode = "62701";
        address.Country = "USA";
        address.IsDefault = true;
        address.Type = AddressType.Billing;

        // Assert
        address.Street.Should().Be("123 Main St");
        address.City.Should().Be("Springfield");
        address.State.Should().Be("IL");
        address.PostalCode.Should().Be("62701");
        address.Country.Should().Be("USA");
        address.IsDefault.Should().BeTrue();
        address.Type.Should().Be(AddressType.Billing);
    }

    [Theory]
    [InlineData(AddressType.Shipping)]
    [InlineData(AddressType.Billing)]
    [InlineData(AddressType.Both)]
    public void Address_Should_SupportAllAddressTypes(AddressType addressType)
    {
        // Arrange
        var address = new Address { Type = addressType };

        // Act & Assert
        address.Type.Should().Be(addressType);
    }

    [Fact]
    public void Address_Should_AllowCompleteAddressCreation()
    {
        // Arrange & Act
        var address = new Address
        {
            Street = "456 Oak Avenue, Apt 3B",
            City = "Chicago",
            State = "Illinois",
            PostalCode = "60601",
            Country = "United States",
            IsDefault = true,
            Type = AddressType.Both
        };

        // Assert
        address.Should().NotBeNull();
        address.Id.Should().NotBeEmpty();
        address.Street.Should().Be("456 Oak Avenue, Apt 3B");
        address.City.Should().Be("Chicago");
        address.State.Should().Be("Illinois");
        address.PostalCode.Should().Be("60601");
        address.Country.Should().Be("United States");
        address.IsDefault.Should().BeTrue();
        address.Type.Should().Be(AddressType.Both);
    }
}

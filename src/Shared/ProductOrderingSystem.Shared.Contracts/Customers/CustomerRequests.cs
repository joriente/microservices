namespace ProductOrderingSystem.Shared.Contracts.Customers;

public record CreateCustomerRequest(
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber);

public record UpdateCustomerRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber);

public record AddAddressRequest(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country,
    bool IsDefault,
    AddressType Type);

public enum AddressType
{
    Shipping,
    Billing,
    Both
}

namespace ProductOrderingSystem.Web.Models;

public record Customer(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    List<Address>? Addresses,
    DateTime CreatedAt)
{
    public string FullName => $"{FirstName} {LastName}";
    public string? Address => Addresses?.FirstOrDefault(a => a.IsDefault)?.Street;
    public string? City => Addresses?.FirstOrDefault(a => a.IsDefault)?.City;
    public string? State => Addresses?.FirstOrDefault(a => a.IsDefault)?.State;
    public string? ZipCode => Addresses?.FirstOrDefault(a => a.IsDefault)?.PostalCode;
}

public record Address(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country,
    bool IsDefault,
    string Type);

public record CreateCustomerRequest(
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber);

public record PaginatedCustomers(
    List<Customer> Customers,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages)
{
    public PaginationMetadata PaginationData => new(Page, PageSize, TotalCount, TotalPages);
}

public record PaginationMetadata(
    int CurrentPage,
    int PageSize,
    int TotalCount,
    int TotalPages);

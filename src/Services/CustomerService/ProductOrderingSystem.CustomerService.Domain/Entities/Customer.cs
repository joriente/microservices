using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ProductOrderingSystem.CustomerService.Domain.Events;
using ProductOrderingSystem.CustomerService.Domain.ValueObjects;

namespace ProductOrderingSystem.CustomerService.Domain.Entities;

public class Customer
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    
    public List<Address> Addresses { get; set; } = new();
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Domain events
    private readonly List<IDomainEvent> _domainEvents = new();
    
    [BsonIgnore]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent eventItem)
    {
        _domainEvents.Add(eventItem);
    }

    public void RemoveDomainEvent(IDomainEvent eventItem)
    {
        _domainEvents.Remove(eventItem);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    // Factory method
    public static Customer Create(string email, string firstName, string lastName, string? phoneNumber = null)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        customer.AddDomainEvent(new CustomerCreatedEvent(
            customer.Id,
            customer.Email,
            customer.FirstName,
            customer.LastName,
            customer.CreatedAt));

        return customer;
    }

    // Business methods
    public void UpdateDetails(string firstName, string lastName, string? phoneNumber)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new CustomerUpdatedEvent(
            Id,
            Email,
            FirstName,
            LastName,
            UpdatedAt));
    }

    public void AddAddress(Address address)
    {
        // If this is the first address or marked as default, make it default
        if (Addresses.Count == 0 || address.IsDefault)
        {
            // Remove default from all other addresses
            foreach (var existingAddress in Addresses)
            {
                existingAddress.IsDefault = false;
            }
            address.IsDefault = true;
        }

        Addresses.Add(address);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new AddressAddedEvent(
            Id,
            address.Street,
            address.City,
            address.State,
            address.PostalCode,
            address.Country,
            UpdatedAt));
    }

    public void SetDefaultAddress(Guid addressId)
    {
        var address = Addresses.FirstOrDefault(a => a.Id == addressId);
        if (address == null)
            throw new InvalidOperationException("Address not found");

        // Remove default from all addresses
        foreach (var addr in Addresses)
        {
            addr.IsDefault = false;
        }

        address.IsDefault = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public string GetFullName() => $"{FirstName} {LastName}";
}

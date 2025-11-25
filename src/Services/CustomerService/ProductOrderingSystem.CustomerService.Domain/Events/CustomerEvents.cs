namespace ProductOrderingSystem.CustomerService.Domain.Events;

public record CustomerCreatedEvent(
    Guid CustomerId,
    string Email,
    string FirstName,
    string LastName,
    DateTime OccurredOn) : IDomainEvent;

public record CustomerUpdatedEvent(
    Guid CustomerId,
    string Email,
    string FirstName,
    string LastName,
    DateTime OccurredOn) : IDomainEvent;

public record AddressAddedEvent(
    Guid CustomerId,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country,
    DateTime OccurredOn) : IDomainEvent;

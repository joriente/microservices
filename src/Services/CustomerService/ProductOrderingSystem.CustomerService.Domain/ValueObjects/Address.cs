using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProductOrderingSystem.CustomerService.Domain.ValueObjects;

public class Address
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public AddressType Type { get; set; } = AddressType.Shipping;
}

public enum AddressType
{
    Shipping,
    Billing,
    Both
}

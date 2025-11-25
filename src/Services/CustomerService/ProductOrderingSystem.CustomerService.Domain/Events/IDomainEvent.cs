namespace ProductOrderingSystem.CustomerService.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}

using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using ProductOrderingSystem.CustomerService.Domain.Entities;
using ProductOrderingSystem.CustomerService.Domain.Events;
using ProductOrderingSystem.CustomerService.Domain.Repositories;
using ProductOrderingSystem.Shared.Contracts.Events;

namespace ProductOrderingSystem.CustomerService.Infrastructure.Behaviors;

/// <summary>
/// Pipeline behavior that publishes domain events as integration events after command execution
/// </summary>
public class DomainEventPublisherBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<DomainEventPublisherBehavior<TRequest, TResponse>> _logger;

    public DomainEventPublisherBehavior(
        IPublishEndpoint publishEndpoint,
        ILogger<DomainEventPublisherBehavior<TRequest, TResponse>> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Execute the handler
        var response = await next();

        // If response contains a Customer entity (through reflection or pattern matching)
        // publish its domain events
        if (response is not null)
        {
            await PublishDomainEventsIfPresent(response, cancellationToken);
        }

        return response;
    }

    private async Task PublishDomainEventsIfPresent(object response, CancellationToken cancellationToken)
    {
        // This is a simplified approach - in production you might want more sophisticated event handling
        // For now, we'll need to capture domain events at the repository level or through a custom approach
    }
}

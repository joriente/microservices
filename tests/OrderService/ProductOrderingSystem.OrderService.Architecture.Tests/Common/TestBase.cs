using System.Reflection;
using ProductOrderingSystem.OrderService.Application.Commands.Orders;
using ProductOrderingSystem.OrderService.Domain.Entities;
using ProductOrderingSystem.OrderService.Infrastructure.Repositories;
using ProductOrderingSystem.OrderService.WebAPI.Endpoints;

namespace ProductOrderingSystem.OrderService.Architecture.Tests.Common;

public abstract class TestBase
{
    protected static readonly Assembly DomainAssembly = typeof(Order).Assembly;
    protected static readonly Assembly ApplicationAssembly = typeof(CreateOrderCommand).Assembly;
    protected static readonly Assembly InfrastructureAssembly = typeof(OrderRepository).Assembly;
    protected static readonly Assembly WebApiAssembly = typeof(OrderEndpoints).Assembly;
    
    // Namespace constants for architecture rules
    protected const string DomainNamespace = "ProductOrderingSystem.OrderService.Domain";
    protected const string ApplicationNamespace = "ProductOrderingSystem.OrderService.Application";
    protected const string InfrastructureNamespace = "ProductOrderingSystem.OrderService.Infrastructure";
    protected const string WebApiNamespace = "ProductOrderingSystem.OrderService.WebAPI";
}

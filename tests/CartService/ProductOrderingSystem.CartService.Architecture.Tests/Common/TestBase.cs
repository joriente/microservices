using System.Reflection;
using ProductOrderingSystem.CartService.Application.Commands.Carts;
using ProductOrderingSystem.CartService.Domain.Entities;
using ProductOrderingSystem.CartService.Infrastructure.Repositories;

namespace ProductOrderingSystem.CartService.Architecture.Tests.Common;

public abstract class TestBase
{
    protected static readonly Assembly DomainAssembly = typeof(Cart).Assembly;
    protected static readonly Assembly ApplicationAssembly = typeof(CreateCartCommand).Assembly;
    protected static readonly Assembly InfrastructureAssembly = typeof(CartRepository).Assembly;
    protected static readonly Assembly WebApiAssembly = AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(a => a.GetName().Name == "ProductOrderingSystem.CartService.WebAPI") 
        ?? Assembly.LoadFrom(typeof(TestBase).Assembly.Location.Replace("ProductOrderingSystem.CartService.Architecture.Tests.dll", "ProductOrderingSystem.CartService.WebAPI.dll"));
    
    // Namespace constants for architecture rules
    protected const string DomainNamespace = "ProductOrderingSystem.CartService.Domain";
    protected const string ApplicationNamespace = "ProductOrderingSystem.CartService.Application";
    protected const string InfrastructureNamespace = "ProductOrderingSystem.CartService.Infrastructure";
    protected const string WebApiNamespace = "ProductOrderingSystem.CartService.WebAPI";
}

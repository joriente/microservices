using System.Reflection;
using ProductOrderingSystem.IdentityService.Application.Commands.Auth;
using ProductOrderingSystem.IdentityService.Domain.Entities;
using ProductOrderingSystem.IdentityService.Infrastructure.Repositories;

namespace ProductOrderingSystem.IdentityService.Architecture.Tests.Common;

public abstract class TestBase
{
    protected static readonly Assembly DomainAssembly = typeof(User).Assembly;
    protected static readonly Assembly ApplicationAssembly = typeof(RegisterUserCommand).Assembly;
    protected static readonly Assembly InfrastructureAssembly = typeof(UserRepository).Assembly;
    protected static readonly Assembly WebApiAssembly = AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(a => a.GetName().Name == "ProductOrderingSystem.IdentityService.WebAPI") 
        ?? Assembly.LoadFrom(typeof(TestBase).Assembly.Location.Replace("ProductOrderingSystem.IdentityService.Architecture.Tests.dll", "ProductOrderingSystem.IdentityService.WebAPI.dll"));
    
    // Namespace constants for architecture rules
    protected const string DomainNamespace = "ProductOrderingSystem.IdentityService.Domain";
    protected const string ApplicationNamespace = "ProductOrderingSystem.IdentityService.Application";
    protected const string InfrastructureNamespace = "ProductOrderingSystem.IdentityService.Infrastructure";
    protected const string WebApiNamespace = "ProductOrderingSystem.IdentityService.WebAPI";
}

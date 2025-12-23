using System.Reflection;
using ProductOrderingSystem.CustomerService.Application.Customers.Commands.CreateCustomer;
using ProductOrderingSystem.CustomerService.Domain.Entities;
using ProductOrderingSystem.CustomerService.Infrastructure.Repositories;

namespace ProductOrderingSystem.CustomerService.Architecture.Tests.Common;

public abstract class TestBase
{
    protected static readonly Assembly DomainAssembly = typeof(Customer).Assembly;
    protected static readonly Assembly ApplicationAssembly = typeof(CreateCustomerCommand).Assembly;
    protected static readonly Assembly InfrastructureAssembly = typeof(MongoCustomerRepository).Assembly;
    protected static readonly Assembly WebApiAssembly = AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(a => a.GetName().Name == "ProductOrderingSystem.CustomerService.WebAPI") 
        ?? Assembly.LoadFrom(typeof(TestBase).Assembly.Location.Replace("ProductOrderingSystem.CustomerService.Architecture.Tests.dll", "ProductOrderingSystem.CustomerService.WebAPI.dll"));
    
    // Namespace constants for architecture rules
    protected const string DomainNamespace = "ProductOrderingSystem.CustomerService.Domain";
    protected const string ApplicationNamespace = "ProductOrderingSystem.CustomerService.Application";
    protected const string InfrastructureNamespace = "ProductOrderingSystem.CustomerService.Infrastructure";
    protected const string WebApiNamespace = "ProductOrderingSystem.CustomerService.WebAPI";
}

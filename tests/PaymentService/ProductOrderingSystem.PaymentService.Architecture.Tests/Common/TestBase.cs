using System.Reflection;
using ProductOrderingSystem.PaymentService.Application.Commands;
using ProductOrderingSystem.PaymentService.Domain.Entities;
using ProductOrderingSystem.PaymentService.Infrastructure.Persistence;

namespace ProductOrderingSystem.PaymentService.Architecture.Tests.Common;

public abstract class TestBase
{
    protected static readonly Assembly DomainAssembly = typeof(Payment).Assembly;
    protected static readonly Assembly ApplicationAssembly = typeof(ProcessPaymentCommand).Assembly;
    protected static readonly Assembly InfrastructureAssembly = typeof(PaymentRepository).Assembly;
    protected static readonly Assembly WebApiAssembly = AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(a => a.GetName().Name == "ProductOrderingSystem.PaymentService.WebAPI") 
        ?? Assembly.LoadFrom(typeof(TestBase).Assembly.Location.Replace("ProductOrderingSystem.PaymentService.Architecture.Tests.dll", "ProductOrderingSystem.PaymentService.WebAPI.dll"));
    
    // Namespace constants for architecture rules
    protected const string DomainNamespace = "ProductOrderingSystem.PaymentService.Domain";
    protected const string ApplicationNamespace = "ProductOrderingSystem.PaymentService.Application";
    protected const string InfrastructureNamespace = "ProductOrderingSystem.PaymentService.Infrastructure";
    protected const string WebApiNamespace = "ProductOrderingSystem.PaymentService.WebAPI";
}

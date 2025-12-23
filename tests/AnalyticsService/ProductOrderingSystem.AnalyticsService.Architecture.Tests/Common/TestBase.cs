using System.Reflection;
using ProductOrderingSystem.AnalyticsService.Domain.Entities;

namespace ProductOrderingSystem.AnalyticsService.Architecture.Tests.Common;

public abstract class TestBase
{
    protected static readonly Assembly DomainAssembly = typeof(OrderEvent).Assembly;
    protected static readonly Assembly ApplicationAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "ProductOrderingSystem.AnalyticsService.Application") ?? DomainAssembly;
    protected static readonly Assembly InfrastructureAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "ProductOrderingSystem.AnalyticsService.Infrastructure") ?? DomainAssembly;
    protected static readonly Assembly WebApiAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "ProductOrderingSystem.AnalyticsService.WebAPI") ?? InfrastructureAssembly;
    
    // Namespace constants for architecture rules
    protected const string DomainNamespace = "ProductOrderingSystem.AnalyticsService.Domain";
    protected const string ApplicationNamespace = "ProductOrderingSystem.AnalyticsService.Application";
    protected const string InfrastructureNamespace = "ProductOrderingSystem.AnalyticsService.Infrastructure";
    protected const string WebApiNamespace = "ProductOrderingSystem.AnalyticsService.WebAPI";
}

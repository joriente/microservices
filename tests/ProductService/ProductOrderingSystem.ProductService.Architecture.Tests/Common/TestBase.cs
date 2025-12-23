using System.Reflection;
using ProductOrderingSystem.ProductService.Application.Commands.Products;
using ProductOrderingSystem.ProductService.Domain.Entities;
using ProductOrderingSystem.ProductService.Infrastructure.Persistence;
using ProductOrderingSystem.ProductService.WebAPI.Endpoints;

namespace ProductOrderingSystem.ProductService.Architecture.Tests.Common;

public abstract class TestBase
{
    protected static readonly Assembly DomainAssembly = typeof(Product).Assembly;
    protected static readonly Assembly ApplicationAssembly = typeof(CreateProductCommand).Assembly;
    protected static readonly Assembly InfrastructureAssembly = typeof(ProductRepository).Assembly;
    protected static readonly Assembly WebApiAssembly = typeof(ProductEndpoints).Assembly;
    
    // Namespace constants for architecture rules
    protected const string DomainNamespace = "ProductOrderingSystem.ProductService.Domain";
    protected const string ApplicationNamespace = "ProductOrderingSystem.ProductService.Application";
    protected const string InfrastructureNamespace = "ProductOrderingSystem.ProductService.Infrastructure";
    protected const string WebApiNamespace = "ProductOrderingSystem.ProductService.WebAPI";
}
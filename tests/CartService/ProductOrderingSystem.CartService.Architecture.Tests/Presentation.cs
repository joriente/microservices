using System;

namespace ProductOrderingSystem.CartService.Architecture.Tests;

public class Presentation : TestBase
{
    [Fact]
    public void WebAPI_ShouldNotContainRequestResponseDTOs()
    {
        // Request/Response DTOs should be in Shared.Contracts, not in WebAPI layer
        var types = Types.InAssembly(WebApiAssembly)
            .That()
            .HaveNameEndingWith("Request")
            .Or()
            .HaveNameEndingWith("Response")
            .Or()
            .HaveNameEndingWith("Dto")
            .GetTypes();

        types.Should().BeEmpty();
    }

    [Fact]
    public void Endpoints_ShouldBeStaticClasses()
    {
        var types = Types.InAssembly(WebApiAssembly)
            .That()
            .ResideInNamespaceContaining("Endpoints")
            .And()
            .HaveNameEndingWith("Endpoints")
            .GetTypes();

        var nonStaticEndpoints = types
            .Where(t => !t.IsAbstract || !t.IsSealed) // static classes are abstract and sealed
            .ToList();

        nonStaticEndpoints.Should().BeEmpty();
    }

    [Fact]
    public void Middleware_ShouldResideInMiddlewareNamespace()
    {
        var types = Types.InAssembly(WebApiAssembly)
            .That()
            .HaveNameEndingWith("Middleware")
            .GetTypes();

        var misplacedMiddleware = types
            .Where(t => !t.Namespace?.Contains("Middleware") == true)
            .ToList();

        misplacedMiddleware.Should().BeEmpty();
    }
}


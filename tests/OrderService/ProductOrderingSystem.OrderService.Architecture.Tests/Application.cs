using System;

namespace ProductOrderingSystem.OrderService.Architecture.Tests;

public class Application : TestBase
{
    [Fact]
    public void CommandHandlers_ShouldHaveCorrectSuffix()
    {
        var types = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespaceContaining("Commands")
            .And()
            .HaveNameEndingWith("Handler")
            .GetTypes();

        var result = types
            .Where(t => !t.Name.EndsWith("CommandHandler"))
            .ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void QueryHandlers_ShouldHaveCorrectSuffix()
    {
        var types = Types
                .InAssembly(ApplicationAssembly)
                .That()
                .ResideInNamespaceContaining("Queries")
                .And()
                .HaveNameEndingWith("Handler")
                .GetTypes();

        var result = types
            .Where(t => !t.Name.EndsWith("QueryHandler"))
            .ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void Commands_ShouldBeImmutableRecords()
    {
        var types = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespaceContaining("Commands")
            .And()
            .HaveNameEndingWith("Command")
            .GetTypes();

        // Check that commands are either records or have only init-only/readonly properties
        var mutableTypes = types
            .Where(t => 
            {
                // Records are immutable by default
                if (t.GetMethod("<Clone>$") != null) return false; // Record type
                
                // Check if all properties are init-only or have no public setter
                var properties = t.GetProperties();
                return properties.Any(p => 
                    p.CanWrite && 
                    p.SetMethod != null && 
                    p.SetMethod.IsPublic && 
                    !p.SetMethod.ReturnParameter.GetRequiredCustomModifiers().Any(m => m.Name == "IsExternalInit"));
            })
            .ToList();

        mutableTypes.Should().BeEmpty();
    }

    [Fact]
    public void Queries_ShouldBeImmutableRecords()
    {
        var types = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespaceContaining("Queries")
            .And()
            .HaveNameEndingWith("Query")
            .GetTypes();

        // Check that queries are either records or have only init-only/readonly properties
        var mutableTypes = types
            .Where(t => 
            {
                // Records are immutable by default
                if (t.GetMethod("<Clone>$") != null) return false; // Record type
                
                // Check if all properties are init-only or have no public setter
                var properties = t.GetProperties();
                return properties.Any(p => 
                    p.CanWrite && 
                    p.SetMethod != null && 
                    p.SetMethod.IsPublic && 
                    !p.SetMethod.ReturnParameter.GetRequiredCustomModifiers().Any(m => m.Name == "IsExternalInit"));
            })
            .ToList();

        mutableTypes.Should().BeEmpty();
    }

    [Fact]
    public void Handlers_ShouldImplementMediatRInterface()
    {
        var types = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .GetTypes();

        var nonMediatRHandlers = types
            .Where(t => !t.GetInterfaces().Any(i => 
                i.IsGenericType && 
                (i.GetGenericTypeDefinition().Name.Contains("IRequestHandler") ||
                 i.GetGenericTypeDefinition().Name.Contains("INotificationHandler"))))
            .ToList();

        nonMediatRHandlers.Should().BeEmpty();
    }
}

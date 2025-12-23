using System.Reflection;

namespace ProductOrderingSystem.InventoryService.Architecture.Tests;

/// <summary>
/// Tests for Vertical Slice Architecture patterns
/// </summary>
public class VerticalSliceTests : TestBase
{
    [Fact]
    public void Features_ShouldBeOrganizedByFeatureNotLayer()
    {
        // Vertical slice = all code for a feature in one place
        var types = Types.InAssembly(InventoryServiceAssembly)
            .That()
            .ResideInNamespaceContaining("Features")
            .GetTypes();

        // Should not have layer-based organization (Controllers, Services, Repositories folders)
        var layeredTypes = types
            .Where(t => t.Namespace?.Contains("Controllers") == true ||
                       t.Namespace?.Contains("Services") == true ||
                       t.Namespace?.Contains("Repositories") == true)
            .ToList();

        layeredTypes.Should().BeEmpty();
    }

    [Fact]
    public void VerticalSlices_ShouldBeStaticClasses()
    {
        // Each vertical slice should be a static class containing nested types
        var types = Types.InAssembly(InventoryServiceAssembly)
            .That()
            .ResideInNamespaceContaining("Features.Inventory")
            .And()
            .DoNotHaveNameEndingWith("Consumer") // Exclude event consumers
            .GetTypes()
            .Where(t => !t.IsNested) // Only top-level classes
            .ToList();

        var nonStaticSlices = types
            .Where(t => !t.IsAbstract || !t.IsSealed) // static classes are abstract and sealed
            .ToList();

        nonStaticSlices.Should().BeEmpty();
    }

    [Fact]
    public void Queries_ShouldBeImmutableRecords()
    {
        var types = Types.InAssembly(InventoryServiceAssembly)
            .That()
            .ResideInNamespaceContaining("Features")
            .And()
            .HaveNameEndingWith("Query")
            .GetTypes();

        // Check that queries are records with init-only properties
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
    public void Commands_ShouldBeImmutableRecords()
    {
        var types = Types.InAssembly(InventoryServiceAssembly)
            .That()
            .ResideInNamespaceContaining("Features")
            .And()
            .HaveNameEndingWith("Command")
            .GetTypes();

        // Check that commands are records with init-only properties
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
        var handlerTypes = Types.InAssembly(InventoryServiceAssembly)
            .That()
            .ResideInNamespaceContaining("Features")
            .And()
            .HaveNameEndingWith("Handler")
            .GetTypes();

        // All handlers should implement IRequestHandler
        var nonMediatRHandlers = handlerTypes
            .Where(t => !t.GetInterfaces().Any(i => 
                i.IsGenericType && 
                i.GetGenericTypeDefinition().Name.Contains("IRequestHandler")))
            .ToList();

        nonMediatRHandlers.Should().BeEmpty();
    }

    [Fact]
    public void Handlers_ShouldBePublicClasses()
    {
        var types = Types.InAssembly(InventoryServiceAssembly)
            .That()
            .ResideInNamespaceContaining("Features")
            .And()
            .HaveNameEndingWith("Handler")
            .GetTypes();

        var nonPublicHandlers = types
            .Where(t => !t.IsPublic && !t.IsNestedPublic)
            .ToList();

        nonPublicHandlers.Should().BeEmpty();
    }

    [Fact]
    public void ResponseDTOs_ShouldBeImmutableRecords()
    {
        var types = Types.InAssembly(InventoryServiceAssembly)
            .That()
            .ResideInNamespaceContaining("Features")
            .And()
            .HaveNameEndingWith("Response")
            .GetTypes();

        // Check that responses are records with init-only properties
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
    public void EventConsumers_ShouldBeInEventConsumersNamespace()
    {
        var types = Types.InAssembly(InventoryServiceAssembly)
            .That()
            .HaveNameEndingWith("Consumer")
            .GetTypes();

        var misplacedConsumers = types
            .Where(t => !t.Namespace?.Contains("EventConsumers") == true)
            .ToList();

        misplacedConsumers.Should().BeEmpty();
    }

    [Fact]
    public void Models_ShouldNotContainComplexBusinessLogic()
    {
        // Models can have simple methods (Reserve, Release) but not complex orchestration
        // This is acceptable in vertical slice architecture
        var types = Types.InAssembly(InventoryServiceAssembly)
            .That()
            .ResideInNamespaceContaining("Models")
            .GetTypes();

        var modelsWithComplexLogic = types
            .Where(t => 
            {
                var methods = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => !m.IsSpecialName && // Exclude property getters/setters
                               !m.Name.StartsWith("get_") && 
                               !m.Name.StartsWith("set_") &&
                               m.Name != "ToString" &&
                               m.Name != "GetHashCode" &&
                               m.Name != "Equals")
                    .ToList();

                // Complex logic indicators: dependency injection, multiple external calls
                return methods.Any(m => 
                    m.GetParameters().Any(p => 
                        p.ParameterType.Namespace?.Contains("Data") == true ||
                        p.ParameterType.Namespace?.Contains("Services") == true));
            })
            .ToList();

        modelsWithComplexLogic.Should().BeEmpty();
    }
}

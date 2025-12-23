using System.Reflection;
using Xunit.Abstractions;
using ProductOrderingSystem.ProductService.Domain.Common;

namespace ProductOrderingSystem.ProductService.Architecture.Tests;

public class DomainModel(ITestOutputHelper output) : TestBase
{
    private static readonly Type Entity = typeof(BaseEntity);
    private static readonly Type DomainEvent = typeof(IDomainEvent);

    [Fact]
    public void DomainModel_ShouldInheritsBaseClasses()
    {
        // Arrange - use Assembly.GetTypes() to avoid NetArchTest Coverlet issues
        var domainModels = DomainAssembly.GetTypes()
            .Where(t => !t.FullName?.Contains("Coverlet.Core.Instrumentation.Tracker") == true)
            .Where(t => !t.Name.Contains("Id") &&
                       !t.Name.Contains("Vogen") &&
                       t.Name != "ThrowHelper" &&
                       !t.Name.EndsWith("Spec") &&
                       !t.Name.EndsWith("Errors") &&
                       !t.Name.EndsWith("Exception") && // Exclude exceptions
                       t.Name != "BaseEntity" && // Exclude base classes themselves
                       t.Name != "DomainEvent" &&
                       !t.IsInterface &&
                       !t.IsEnum &&
                       !t.IsAbstract && // Exclude abstract base classes
                       t.Namespace?.StartsWith("ProductOrderingSystem.ProductService.Domain") == true)
            .ToList();

        domainModels.Dump(output);

        // Act
        var failingTypes = domainModels
            .Where(t => !t.IsSubclassOf(Entity) && !typeof(IDomainEvent).IsAssignableFrom(t))
            .ToList();

        // Assert
        failingTypes.Should().BeEmpty();
    }

    [Fact]
    public void EntitiesAndAggregates_ShouldHavePrivateParameterlessConstructor()
    {
        var entityTypes = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(Entity)
            .GetTypes()
            .Where(t => t != Entity);

        var failingTypes = new List<Type>();

        foreach (var entityType in entityTypes)
        {
            var constructors = entityType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);

            if (!constructors.Any(c => c.IsPrivate && c.GetParameters().Length == 0))
                failingTypes.Add(entityType);

            failingTypes.Should().BeEmpty();
        }
    }

    [Fact]
    public void DomainEvents_ShouldFollowNamingConvention()
    {
        var types = Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(DomainEvent)
            .And()
            .DoNotResideInNamespaceContaining("Common")
            .GetTypes();

        var invalidNames = types
            .Where(t => !t.Name.EndsWith("Event"))
            .ToList();

        invalidNames.Should().BeEmpty();
    }

    [Fact]
    public void Repositories_ShouldBeInterfacesInDomain()
    {
        var types = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespaceContaining("Repositories")
            .GetTypes();

        var nonInterfaces = types
            .Where(t => !t.IsInterface)
            .ToList();

        nonInterfaces.Should().BeEmpty();
    }

    [Fact]
    public void DomainLayer_ShouldNotReferenceInfrastructureConcerns()
    {
        // Filter out Coverlet instrumentation types that cause TypeLoadException in CI
        var types = DomainAssembly.GetTypes()
            .Where(t => !t.FullName?.Contains("Coverlet.Core.Instrumentation.Tracker") == true)
            .ToList();

        var infrastructureDependencies = new List<Type>();
        
        foreach (var type in types)
        {
            try
            {
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (fields.Any(f => f.FieldType.Namespace?.Contains("MongoDB") == true ||
                                   f.FieldType.Namespace?.Contains("RabbitMQ") == true ||
                                   f.FieldType.Namespace?.Contains("EntityFramework") == true ||
                                   f.FieldType.Namespace?.Contains("Dapper") == true))
                {
                    infrastructureDependencies.Add(type);
                }
            }
            catch (TypeLoadException)
            {
                // Skip types that can't be loaded
            }
        }

        infrastructureDependencies.Should().BeEmpty();
    }
}

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
        // Arrange
        var domainModels = Types.InAssembly(DomainAssembly)
            .That()
            .DoNotResideInNamespaceContaining("Common")
            .And().DoNotResideInNamespaceContaining("Repositories")
            .And().DoNotResideInNamespaceContaining("Exceptions")
            .And().DoNotHaveNameMatching(".*Id.*")
            .And().DoNotHaveNameMatching(".*Vogen.*")
            .And().DoNotHaveName("ThrowHelper")
            .And().DoNotHaveNameEndingWith("Spec")
            .And().DoNotHaveNameEndingWith("Errors")
            .And().AreNotInterfaces()
            .GetTypes()
            .Where(t => !t.IsEnum)
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
        var types = Types.InAssembly(DomainAssembly)
            .GetTypes();

        var infrastructureDependencies = types
            .Where(t => t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Any(f => f.FieldType.Namespace?.Contains("MongoDB") == true ||
                         f.FieldType.Namespace?.Contains("RabbitMQ") == true ||
                         f.FieldType.Namespace?.Contains("EntityFramework") == true ||
                         f.FieldType.Namespace?.Contains("Dapper") == true))
            .ToList();

        infrastructureDependencies.Should().BeEmpty();
    }
}

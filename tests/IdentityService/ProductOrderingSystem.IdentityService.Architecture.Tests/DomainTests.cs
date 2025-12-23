using System.Reflection;
using Xunit.Abstractions;

namespace ProductOrderingSystem.IdentityService.Architecture.Tests;

public class DomainModel(ITestOutputHelper output) : TestBase
{
    [Fact]
    public void DomainEvents_ShouldFollowNamingConvention()
    {
        var types = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespaceContaining("Events")
            .GetTypes();

        var invalidNames = types
            .Where(t => !t.Name.EndsWith("Event") && !t.IsInterface && t.Name != "DomainEvent")
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


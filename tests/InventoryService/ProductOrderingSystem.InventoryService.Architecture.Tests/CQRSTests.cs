namespace ProductOrderingSystem.InventoryService.Architecture.Tests;

/// <summary>
/// Tests for MediatR CQRS patterns
/// </summary>
public class CQRSTests : TestBase
{
    [Fact]
    public void Queries_ShouldNotModifyState()
    {
        // Query handlers should only read data, not modify it
        // We can check method names for hints of state modification
        var queryHandlers = Types.InAssembly(InventoryServiceAssembly)
            .That()
            .ResideInNamespaceContaining("Features")
            .And()
            .HaveNameEndingWith("Handler")
            .GetTypes()
            .Where(t => t.DeclaringType?.Name.Contains("Query") == true ||
                       t.GetInterfaces().Any(i => i.GetGenericArguments().Any(arg => arg.Name.Contains("Query"))))
            .ToList();

        var modifyingQueryHandlers = new List<Type>();

        foreach (var handler in queryHandlers)
        {
            var methods = handler.GetMethods();
            var hasModifyingMethods = methods.Any(m => 
                m.Name.Contains("Add") ||
                m.Name.Contains("Update") ||
                m.Name.Contains("Delete") ||
                m.Name.Contains("Remove") ||
                m.Name.Contains("Save") ||
                m.Name.Contains("Insert"));

            if (hasModifyingMethods)
                modifyingQueryHandlers.Add(handler);
        }

        modifyingQueryHandlers.Should().BeEmpty();
    }

    [Fact]
    public void Commands_ShouldFollowNamingConvention()
    {
        var commands = Types.InAssembly(InventoryServiceAssembly)
            .That()
            .ResideInNamespaceContaining("Features")
            .And()
            .HaveNameEndingWith("Command")
            .GetTypes();

        // Command names should be imperative (verbs)
        var invalidNames = commands
            .Where(c => !c.Name.Any(char.IsUpper) || c.Name.Length < 3)
            .ToList();

        invalidNames.Should().BeEmpty();
    }

    [Fact]
    public void Queries_ShouldFollowNamingConvention()
    {
        var queries = Types.InAssembly(InventoryServiceAssembly)
            .That()
            .ResideInNamespaceContaining("Features")
            .And()
            .HaveNameEndingWith("Query")
            .GetTypes();

        // Query names should describe what they retrieve
        var invalidNames = queries
            .Where(q => !q.Name.Any(char.IsUpper) || q.Name.Length < 3)
            .ToList();

        invalidNames.Should().BeEmpty();
    }
}

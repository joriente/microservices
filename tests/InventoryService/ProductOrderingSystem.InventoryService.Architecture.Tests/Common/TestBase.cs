using System.Reflection;
using ProductOrderingSystem.InventoryService.Features.Inventory;

namespace ProductOrderingSystem.InventoryService.Architecture.Tests.Common;

public abstract class TestBase
{
    // Single assembly for vertical slice architecture
    protected static readonly Assembly InventoryServiceAssembly = typeof(GetInventoryByProductId).Assembly;
    
    // Namespace constants
    protected const string FeaturesNamespace = "ProductOrderingSystem.InventoryService.Features";
    protected const string ModelsNamespace = "ProductOrderingSystem.InventoryService.Models";
    protected const string DataNamespace = "ProductOrderingSystem.InventoryService.Data";
}

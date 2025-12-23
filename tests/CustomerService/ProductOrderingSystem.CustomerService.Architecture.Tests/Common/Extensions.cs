using System.Diagnostics;
using Xunit.Abstractions;

namespace ProductOrderingSystem.CustomerService.Architecture.Tests.Common;

public static class Extensions
{
    public static void Dump(this IEnumerable<Type> types, ITestOutputHelper output)
    {
        foreach (var type in types)
        {
            output.WriteLine(type.FullName);
        }
    }
}

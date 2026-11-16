using System.Reflection;

namespace Optionary;

/// <summary>Reads the <c>[AssemblyMetadata]</c> the test project's build wrote into it (see PackageUnderTest.targets).</summary>
internal static class TestAssemblyMetadata
{
    public static string Get(string key) =>
        typeof(TestAssemblyMetadata).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(z => z.Key == key).Value!;
}

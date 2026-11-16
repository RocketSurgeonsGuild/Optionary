using System.Xml.Linq;

namespace Optionary.Configuration.Json.Tests;

/// <summary>
///     What the Optionary.Configuration.Json package ships: the three Dovetail joint templates with the build targets that declare them,
///     an assembly named after the package, and one dependency, Microsoft.Extensions.Configuration.Json (decision D12).
/// </summary>
public sealed class PackageContentTests
{
    [Test]
    public async Task ThePackage_ShipsTheJointTemplatesAndBuildAssets()
    {
        using var package = JsonPackage.Open();

        var buildAssets = package.Entries.Select(z => z.FullName)
                                 .Where(z => z.StartsWith("build", StringComparison.Ordinal))
                                 .Order(StringComparer.Ordinal)
                                 .ToArray();

        await buildAssets.Should().BeEquivalentTo(
            [
                "build/JsonBrowserJoint.cs.template",
                "build/JsonEmbeddedJoint.cs.template",
                "build/JsonJoint.cs.template",
                "build/Optionary.Configuration.Json.props",
                "build/Optionary.Configuration.Json.targets",
                "buildMultiTargeting/Optionary.Configuration.Json.props",
                "buildMultiTargeting/Optionary.Configuration.Json.targets",
                "buildTransitive/Optionary.Configuration.Json.props",
                "buildTransitive/Optionary.Configuration.Json.targets",
            ],
            CollectionOrdering.Matching
        );
    }

    /// <summary>
    ///     The package has no API of its own, but it still ships an assembly named after it: Dovetail emits a template whose
    ///     <c>Package</c> is set only when that assembly is among the project's resolved references.
    /// </summary>
    [Test]
    public async Task ThePackage_ShipsAnAssemblyNamedAfterItForEachTargetFramework()
    {
        using var package = JsonPackage.Open();

        var libraries = package.Entries.Select(z => z.FullName)
                               .Where(z => z.StartsWith("lib/", StringComparison.Ordinal) && z.EndsWith(".dll", StringComparison.Ordinal))
                               .Order(StringComparer.Ordinal)
                               .ToArray();

        await libraries.Should().BeEquivalentTo(
            ["lib/net10.0/Optionary.Configuration.Json.dll", "lib/net11.0/Optionary.Configuration.Json.dll"],
            CollectionOrdering.Matching
        );
    }

    [Test]
    public async Task ThePackage_DependsOnlyOnTheJsonConfigurationPackage_NotOnOptionary()
    {
        var nuspec = await ReadNuspec();

        var dependencies = nuspec.Descendants()
                                 .Where(z => z.Name.LocalName == "dependency")
                                 .Select(z => (string)z.Attribute("id")!)
                                 .Distinct(StringComparer.Ordinal)
                                 .ToArray();

        await dependencies.Should().BeEquivalentTo(["Microsoft.Extensions.Configuration.Json"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task ThePackageDescription_SaysTheTemplatesNeedDovetail()
    {
        var nuspec = await ReadNuspec();

        var description = nuspec.Descendants().Single(z => z.Name.LocalName == "description").Value;

        await description.Should().Contain("do nothing without Dovetail");
    }

    private static async Task<XDocument> ReadNuspec()
    {
        using var package = JsonPackage.Open();
        var nuspecEntry = package.Entries.Single(z => z.FullName == $"{JsonPackage.Id}.nuspec");
        await using var nuspecStream = await nuspecEntry.OpenAsync();
        return await XDocument.LoadAsync(nuspecStream, LoadOptions.None, CancellationToken.None);
    }
}

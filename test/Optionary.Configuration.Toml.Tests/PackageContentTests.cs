using System.Xml.Linq;

namespace Optionary.Configuration.Toml.Tests;

/// <summary>
///     What the Optionary.Configuration.Toml package ships: the three Dovetail joint templates with the build targets that declare them,
///     an assembly named after the package, and one dependency, the Tomlyn.Extensions.Configuration provider (decision D12).
/// </summary>
public sealed class PackageContentTests
{
    [Test]
    public async Task ThePackage_ShipsTheJointTemplatesAndBuildAssets()
    {
        using var package = TomlPackage.Open();

        var buildAssets = package.Entries.Select(z => z.FullName)
                                 .Where(z => z.StartsWith("build", StringComparison.Ordinal))
                                 .Order(StringComparer.Ordinal)
                                 .ToArray();

        await buildAssets.Should().BeEquivalentTo(
            [
                "build/Optionary.Configuration.Toml.props",
                "build/Optionary.Configuration.Toml.targets",
                "build/TomlBrowserJoint.cs.template",
                "build/TomlEmbeddedJoint.cs.template",
                "build/TomlJoint.cs.template",
                "buildMultiTargeting/Optionary.Configuration.Toml.props",
                "buildMultiTargeting/Optionary.Configuration.Toml.targets",
                "buildTransitive/Optionary.Configuration.Toml.props",
                "buildTransitive/Optionary.Configuration.Toml.targets",
            ],
            CollectionOrdering.Matching
        );
    }

    /// <summary>
    ///     Dovetail emits a template whose <c>Package</c> is set only into a project whose resolved references include an assembly named
    ///     after that package, so the package ships one even though it has no API of its own.
    /// </summary>
    [Test]
    public async Task ThePackage_ShipsAnAssemblyNamedAfterItForEachTargetFramework()
    {
        using var package = TomlPackage.Open();

        var libraries = package.Entries.Select(z => z.FullName)
                               .Where(z => z.StartsWith("lib/", StringComparison.Ordinal) && z.EndsWith(".dll", StringComparison.Ordinal))
                               .Order(StringComparer.Ordinal)
                               .ToArray();

        await libraries.Should().BeEquivalentTo(
            ["lib/net10.0/Optionary.Configuration.Toml.dll", "lib/net11.0/Optionary.Configuration.Toml.dll"],
            CollectionOrdering.Matching
        );
    }

    [Test]
    public async Task ThePackage_DependsOnlyOnTomlynExtensionsConfiguration()
    {
        var nuspec = await ReadNuspec();

        var dependencies = nuspec.Descendants()
                                 .Where(z => z.Name.LocalName == "dependency")
                                 .Select(z => (string)z.Attribute("id")!)
                                 .Distinct(StringComparer.Ordinal)
                                 .ToArray();

        await dependencies.Should().BeEquivalentTo(["Tomlyn.Extensions.Configuration"], CollectionOrdering.Matching);
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
        using var package = TomlPackage.Open();
        var nuspecEntry = package.Entries.Single(z => z.FullName == $"{TomlPackage.Id}.nuspec");
        await using var nuspecStream = await nuspecEntry.OpenAsync();
        return await XDocument.LoadAsync(nuspecStream, LoadOptions.None, CancellationToken.None);
    }
}

using System.Xml.Linq;

namespace Optionary.Configuration.Yaml.Tests;

/// <summary>
///     What the Optionary.Configuration.Yaml package ships: the provider, the three Dovetail joint templates with the build targets that
///     declare them, and no dependency on Optionary (decision D12).
/// </summary>
public sealed class PackageContentTests
{
    [Test]
    public async Task ThePackage_ShipsTheJointTemplatesAndBuildAssets()
    {
        using var package = YamlPackage.Open();

        var buildAssets = package.Entries.Select(z => z.FullName)
                                 .Where(z => z.StartsWith("build", StringComparison.Ordinal))
                                 .Order(StringComparer.Ordinal)
                                 .ToArray();

        await buildAssets.Should().BeEquivalentTo(
            [
                "build/Optionary.Configuration.Yaml.props",
                "build/Optionary.Configuration.Yaml.targets",
                "build/YamlBrowserJoint.cs.template",
                "build/YamlEmbeddedJoint.cs.template",
                "build/YamlJoint.cs.template",
                "buildMultiTargeting/Optionary.Configuration.Yaml.props",
                "buildMultiTargeting/Optionary.Configuration.Yaml.targets",
                "buildTransitive/Optionary.Configuration.Yaml.props",
                "buildTransitive/Optionary.Configuration.Yaml.targets",
            ],
            CollectionOrdering.Matching
        );
    }

    [Test]
    public async Task ThePackage_ShipsTheProviderForEachTargetFramework()
    {
        using var package = YamlPackage.Open();

        var libraries = package.Entries.Select(z => z.FullName)
                               .Where(z => z.StartsWith("lib/", StringComparison.Ordinal) && z.EndsWith(".dll", StringComparison.Ordinal))
                               .Order(StringComparer.Ordinal)
                               .ToArray();

        await libraries.Should().BeEquivalentTo(
            ["lib/net10.0/Optionary.Configuration.Yaml.dll", "lib/net11.0/Optionary.Configuration.Yaml.dll"],
            CollectionOrdering.Matching
        );
    }

    [Test]
    public async Task ThePackage_DependsOnTheConfigurationPackagesAndYamlDotNet_NotOnOptionary()
    {
        using var package = YamlPackage.Open();
        var nuspecEntry = package.Entries.Single(z => z.FullName == $"{YamlPackage.Id}.nuspec");
        await using var nuspecStream = await nuspecEntry.OpenAsync();
        var nuspec = await XDocument.LoadAsync(nuspecStream, LoadOptions.None, CancellationToken.None);

        var dependencies = nuspec.Descendants()
                                 .Where(z => z.Name.LocalName == "dependency")
                                 .Select(z => (string)z.Attribute("id")!)
                                 .Distinct(StringComparer.Ordinal)
                                 .Order(StringComparer.Ordinal)
                                 .ToArray();

        await dependencies.Should().BeEquivalentTo(
            ["Microsoft.Extensions.Configuration", "Microsoft.Extensions.Configuration.FileExtensions", "YamlDotNet"],
            CollectionOrdering.Matching
        );
    }

    [Test]
    public async Task ThePackageDescription_SaysTheTemplatesNeedDovetail()
    {
        using var package = YamlPackage.Open();
        var nuspecEntry = package.Entries.Single(z => z.FullName == $"{YamlPackage.Id}.nuspec");
        await using var nuspecStream = await nuspecEntry.OpenAsync();
        var nuspec = await XDocument.LoadAsync(nuspecStream, LoadOptions.None, CancellationToken.None);

        var description = nuspec.Descendants().Single(z => z.Name.LocalName == "description").Value;

        await description.Should().Contain("do nothing without Dovetail");
    }
}

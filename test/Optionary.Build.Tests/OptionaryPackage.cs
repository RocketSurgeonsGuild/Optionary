using System.Xml.Linq;

namespace Optionary.Build.Tests;

/// <summary>
///     The Optionary package this test project packed when it was built (see the project file), and the repository it came from.
/// </summary>
internal static class OptionaryPackage
{
    public const string Id = "Optionary";

    public static string Directory { get; } = TestAssemblyMetadata.Get("PackageUnderTestDirectory");

    public static string RepositoryRoot { get; } = TestAssemblyMetadata.Get("OptionaryRepositoryRoot");

    public static string Path { get; } = System.IO.Directory.EnumerateFiles(Directory, $"{Id}.*.nupkg")
                                                 .Single(z => char.IsDigit(System.IO.Path.GetFileName(z)[Id.Length + 1]));

    /// <summary>The package version, from the file name.</summary>
    public static string Version { get; } = System.IO.Path.GetFileNameWithoutExtension(Path)[( Id.Length + 1 )..];

    /// <summary>
    ///     The version <c>Directory.Packages.props</c> pins for <paramref name="packageId" /> under <paramref name="targetFramework" />,
    ///     so scaffolded projects reference what the repository builds against. Packages split per release train sit in two item
    ///     groups conditioned on net11.0 compatibility.
    /// </summary>
    public static string CentralPackageVersion(string packageId, string targetFramework)
    {
        var path = System.IO.Path.Combine(RepositoryRoot, "Directory.Packages.props");
        var isNet11OrGreater = System.Version.Parse(targetFramework["net".Length..]).Major >= 11;
        var version = XDocument.Load(path)
                               .Descendants("PackageVersion")
                               .Where(z => string.Equals(z.Attribute("Include")?.Value, packageId, StringComparison.OrdinalIgnoreCase))
                               .FirstOrDefault(
                                    z => z.Parent?.Attribute("Condition")?.Value is not { } condition
                                     || !condition.Contains("IsTargetFrameworkCompatible", StringComparison.Ordinal)
                                     || condition.TrimStart().StartsWith('!') != isNet11OrGreater
                                )
                              ?.Attribute("Version")
                              ?.Value;
        return version ?? throw new InvalidOperationException($"No PackageVersion for {packageId} in {path}.");
    }
}

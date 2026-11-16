using System.IO.Compression;

namespace Optionary.Configuration.Json.Tests;

/// <summary>
///     The Optionary.Configuration.Json package this test project packed when it was built (see the project file).
/// </summary>
internal static class JsonPackage
{
    public const string Id = "Optionary.Configuration.Json";

    public static string Directory { get; } = TestAssemblyMetadata.Get("PackageUnderTestDirectory");

    public static string Path { get; } = System.IO.Directory.EnumerateFiles(Directory, $"{Id}.*.nupkg").Single();

    /// <summary>The package version, from the file name.</summary>
    public static string Version { get; } = System.IO.Path.GetFileNameWithoutExtension(Path)[( Id.Length + 1 )..];

    public static ZipArchive Open() => ZipFile.OpenRead(Path);
}

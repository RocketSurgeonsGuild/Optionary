using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Build.Logging.StructuredLogger;
using Microsoft.Build.Utilities.ProjectCreation;
using TUnit.Core.Exceptions;
using Task = System.Threading.Tasks.Task;

namespace Optionary.Build.Tests;

/// <summary>
///     A package <c>dotnet pack</c> produced: its identity and the files in it (without the zip package's own bookkeeping entries).
/// </summary>
public sealed record PackageVerificationResult(string PackageId, string Version, IReadOnlyList<string> Files);

/// <summary>
///     What a scaffolded project's build left behind: the configuration-shaped files in its <c>bin/</c> and the warnings it logged.
/// </summary>
public sealed record ProjectBuildResult(string Name, IReadOnlyList<string> OutputFiles, IReadOnlyList<string> Warnings);

/// <summary>
///     Scaffolds throwaway projects outside the repository that reference the packed Optionary package, builds them out of process
///     with <c>dotnet</c>, and verifies what they wrote. The scaffold restores from its own feeds (the package under test, the
///     packages it packed itself, and nuget.org) into its own packages folder, so neither the package under test nor its test
///     libraries ever reach the machine's global package cache.
/// </summary>
public sealed class OptionaryTestProject : IDisposable
{
    /// <summary>
    ///     The prefix of the package IDs a scaffold packs itself (<see cref="PackToLocalFeed" />): only the scaffold's own feed may
    ///     serve them, so restore can't pick up an unrelated nuget.org package with the same ID.
    /// </summary>
    public const string LocalPackagePrefix = "OptionaryTest";

    private readonly List<ProjectCreator> _projects = [];
    private readonly VerifySettings _settings;

    public string Directory { get; }

    private string FeedDirectory => Path.Combine(Directory, "feed");

    /// <summary>
    /// 
    /// </summary>
    /// <param name="referenceOptionary">
    ///     Whether the scaffold's <c>Directory.Build.props</c> references the Optionary package for every project. Without it, each
    ///     project references Optionary itself (see <see cref="ReferenceOptionary" />).
    /// </param>
    public OptionaryTestProject(bool referenceOptionary = true)
    {
        var id = Guid.NewGuid().ToString("N");
        // The folder's real path: on macOS the temp folder is under /var, a symlink to /private/var, and MSBuild reports the
        // resolved form for some paths and not others. NuGet guards its packages folder with locks named after the path, so two
        // spellings of the scaffold's packages folder let one restore extract a package while another reads it half-written.
        Directory = System.IO.Directory.CreateDirectory(Path.Combine(ResolveSymbolicLinks(Path.GetTempPath()), "optionary-build-tests", id)).FullName;
        System.IO.Directory.CreateDirectory(FeedDirectory);

        AddFile("global.json", File.ReadAllText(Path.Combine(OptionaryPackage.RepositoryRoot, "global.json")));
        AddFile(
            "NuGet.config",
            $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="optionary" value="{OptionaryPackage.Directory}" />
                <add key="scaffold" value="{FeedDirectory}" />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
              <packageSourceMapping>
                <clear />
                <packageSource key="optionary">
                  <package pattern="{OptionaryPackage.Id}" />
                </packageSource>
                <packageSource key="scaffold">
                  <package pattern="{LocalPackagePrefix}*" />
                </packageSource>
                <packageSource key="nuget.org">
                  <package pattern="*" />
                </packageSource>
              </packageSourceMapping>
            </configuration>
            """
        );
        AddFile(
            "Directory.Build.props",
            $"""
            <Project>
              <PropertyGroup>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <RestorePackagesPath>$(MSBuildThisFileDirectory).packages</RestorePackagesPath>
                <NuGetAudit>false</NuGetAudit>
                <!--
                  A prerelease of its own, so packing a scaffolded library that depends on a prerelease Optionary (as CI builds
                  it, e.g. 0.0.1-beta.11) doesn't warn NU5104 "stable release with a prerelease dependency".
                -->
                <Version>1.0.0-scaffold</Version>
              </PropertyGroup>
              <ItemGroup Condition="'{referenceOptionary}' == 'True'">
                <PackageReference Include="{OptionaryPackage.Id}" Version="{OptionaryPackage.Version}" />
              </ItemGroup>
            </Project>
            """
        );

        _settings = new();
        _settings.ScrubMember<PackageVerificationResult>(z => z.Version);
        // The tests run once per target framework (see TargetFrameworksAttribute) against one shared snapshot.
        _settings.IgnoreConstructorParameters();
        // The scaffold lives under the resolved temp folder (/private/var on macOS); Verify's own TempPath scrubber only knows
        // Path.GetTempPath(). That, the id and the target framework go in one pass: separate ScrubLinesWithReplace registrations
        // each run against the original line rather than chaining.
        var tempPath = Path.GetTempPath();
        _settings.ScrubLinesWithReplace(
            z => ScrubTargetFramework(z.Replace("/private" + tempPath, "{TempPath}").Replace(id, "{id}")));
    }

    /// <summary>
    ///     Adds a <c>PackageReference</c> to the Optionary package under test, for a scaffold created without one for every project.
    /// </summary>
    public static ProjectCreator ReferenceOptionary(ProjectCreator project, IDictionary<string, string?>? metadata = null)
    {
        var all = new Dictionary<string, string?>(metadata ?? new Dictionary<string, string?>()) { ["Version"] = OptionaryPackage.Version };
        return project.ItemInclude("PackageReference", OptionaryPackage.Id, metadata: all);
    }

    /// <summary>
    ///     Saves a project file to disk. When <paramref name="track" /> is <see langword="true" /> (the default) and the file is a
    ///     .csproj, it's also registered for <see cref="VerifyProjects" /> / <see cref="VerifyProjectsAndPackages" /> to build. Pass
    ///     <see langword="false" /> for a library that's built only through a <c>ProjectReference</c> from another tracked project.
    ///     Create <paramref name="project" /> without a path: this sets it, and a path given at creation is shared with every other
    ///     test that uses it (see Config.cs).
    /// </summary>
    public OptionaryTestProject AddProject(string path, ProjectCreator project, bool track = true)
    {
        project.Save(Path.Combine(Directory, path));
        if (track && project.FullPath.EndsWith(".csproj", StringComparison.Ordinal))
        {
            _projects.Add(project);
        }

        return this;
    }

    public OptionaryTestProject AddFile(string path, string content)
    {
        var fullPath = Path.Combine(Directory, path);
        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
        return this;
    }

    public string FullPath(string relativePath) => Path.Combine(Directory, relativePath);

    /// <summary>
    ///     Builds every tracked project and verifies its output files and warnings. Passing <paramref name="files" /> (paths
    ///     relative to the scaffold) verifies their text as well (see <see cref="FileTargets" />).
    /// </summary>
    public async Task VerifyProjects(params string[] files)
    {
        var results = new List<ProjectBuildResult>();
        foreach (var project in _projects)
        {
            results.Add(await Build(Path.GetDirectoryName(project.FullPath)!, "build"));
        }

        await VerifyWithFiles(results, files);
    }

    /// <summary>
    ///     Like <see cref="VerifyProjects" />, but packs every tracked project and also verifies the files in each package.
    /// </summary>
    public async Task VerifyProjectsAndPackages(params string[] files)
    {
        var results = new List<ProjectBuildResult>();
        var packages = new List<PackageVerificationResult>();
        foreach (var project in _projects)
        {
            var projectDirectory = Path.GetDirectoryName(project.FullPath)!;
            results.Add(await Build(projectDirectory, "pack"));
            packages.Add(ReadPackage(FindPackage(projectDirectory)));
        }

        await VerifyWithFiles(new { Projects = results, Packages = packages }, files);
    }

    /// <summary>
    ///     Runs <c>dotnet publish</c> for the project at <paramref name="relativePath" /> and verifies which configuration-shaped
    ///     files ended up in its <c>publish/</c> folder: publish assembles its own file set, so a build passing doesn't prove it.
    /// </summary>
    public async Task VerifyPublish(string relativePath, params string[] files)
    {
        var publishFiles = await Publish(relativePath);

        await VerifyWithFiles(new ProjectBuildResult(Path.GetFileName(relativePath), publishFiles, Warnings(relativePath)), files);
    }

    /// <summary>
    ///     Runs <c>dotnet publish</c> for the project at <paramref name="relativePath" /> and returns the configuration-shaped files
    ///     in its <c>publish/</c> folder.
    /// </summary>
    public async Task<IReadOnlyList<string>> Publish(string relativePath)
    {
        var projectDirectory = FullPath(relativePath);
        await RunDotnet("publish -bl", projectDirectory);

        var publishDirectory = System.IO.Directory.EnumerateDirectories(Path.Combine(projectDirectory, "bin"), "publish", SearchOption.AllDirectories).SingleOrDefault();
        return publishDirectory is null ? [] : ListConfigurationFiles(publishDirectory);
    }

    /// <summary>
    ///     The text of a file the scaffold's builds wrote, at <paramref name="relativePath" /> from the scaffold directory.
    /// </summary>
    public string ReadFile(string relativePath)
    {
        var path = FullPath(relativePath);
        return File.Exists(path) ? File.ReadAllText(path) : throw new TUnitException($"{relativePath} doesn't exist.");
    }

    public bool Exists(string relativePath) => File.Exists(FullPath(relativePath));

    /// <summary>
    ///     The text of <paramref name="entry" /> in the package <c>dotnet pack</c> produced for the project at
    ///     <paramref name="relativePath" />.
    /// </summary>
    public string ReadPackageFile(string relativePath, string entry)
    {
        using var archive = ZipFile.OpenRead(FindPackage(FullPath(relativePath)));
        var zipEntry = archive.GetEntry(entry) ?? throw new TUnitException($"The package of {relativePath} has no {entry}.");
        using var reader = new StreamReader(zipEntry.Open());
        return reader.ReadToEnd();
    }

    /// <summary>
    ///     Every warning (<c>code: text</c>) the last build, pack or publish of the project at <paramref name="relativePath" />
    ///     logged, read from its binary log.
    /// </summary>
    public IReadOnlyList<string> Warnings(string relativePath) =>
        ReadWarnings(BinaryLog.ReadBuild(Path.Combine(FullPath(relativePath), "msbuild.binlog")));

    /// <summary>
    ///     Packs the project at <paramref name="relativePath" /> into the scaffold's own feed, so a project added afterwards can
    ///     consume it through a real <c>PackageReference</c>. Its package ID has to start with <see cref="LocalPackagePrefix" />.
    /// </summary>
    public async Task<PackageVerificationResult> PackToLocalFeed(string relativePath)
    {
        var projectDirectory = FullPath(relativePath);
        await Build(projectDirectory, "pack");

        var nupkg = FindPackage(projectDirectory);
        var result = ReadPackage(nupkg);
        if (!result.PackageId.StartsWith(LocalPackagePrefix, StringComparison.Ordinal))
        {
            throw new TUnitException($"{result.PackageId} doesn't start with {LocalPackagePrefix}, so the scaffold's feed can't serve it.");
        }

        File.Copy(nupkg, Path.Combine(FeedDirectory, Path.GetFileName(nupkg)), overwrite: true);
        return result;
    }

    /// <summary>
    ///     Builds and runs the application at <paramref name="relativePath" /> with <c>dotnet run</c> and returns what it wrote to
    ///     standard output.
    /// </summary>
    public Task<string> Run(string relativePath) => RunDotnet("run", FullPath(relativePath), rollForward: true);

    /// <summary>
    ///     Runs <c>dotnet build</c> (plus <paramref name="arguments" />) for the project at <paramref name="relativePath" /> and
    ///     returns the build's binary log, for behaviour that only shows across builds (incrementality, stale outputs).
    /// </summary>
    public async Task<Microsoft.Build.Logging.StructuredLogger.Build> BuildProject(string relativePath, string arguments = "")
    {
        var projectDirectory = FullPath(relativePath);
        await RunDotnet($"build -bl {arguments}", projectDirectory);
        return BinaryLog.ReadBuild(Path.Combine(projectDirectory, "msbuild.binlog"));
    }

    /// <summary>
    ///     Runs <c>dotnet build</c> (plus <paramref name="arguments" />) for the project at <paramref name="relativePath" />,
    ///     expecting it to fail, and returns its output.
    /// </summary>
    public async Task<string> BuildProjectExpectingFailure(string relativePath, string arguments = "")
    {
        var (exitCode, output) = await Dotnet($"build {arguments}", FullPath(relativePath));
        return exitCode != 0 ? output : throw new TUnitException($"dotnet build succeeded for {relativePath}, but it was expected to fail:\n{output}");
    }

    /// <summary>
    ///     Restores the project at <paramref name="relativePath" />, runs <c>dotnet msbuild</c> with <paramref name="msbuildArguments" />
    ///     (say <c>-t:ResolveAssemblyReferences</c>, to see the items targets add, or <c>-p:Name=value</c>), and returns every
    ///     <paramref name="itemType" /> item with its metadata (<c>Identity</c> included).
    /// </summary>
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, string>>> EvaluateItems(string relativePath, string itemType, string msbuildArguments = "")
    {
        var projectDirectory = FullPath(relativePath);
        // Restored separately: `dotnet msbuild -restore` combined with -getItem doesn't restore at all.
        await RunDotnet("restore", projectDirectory);
        // -getItem prints JSON only for more than one value, so always ask for a property too.
        var stdout = await RunDotnet($"msbuild {msbuildArguments} -getItem:{itemType} -getProperty:OutputType", projectDirectory);

        using var document = JsonDocument.Parse(stdout[stdout.IndexOf('{', StringComparison.Ordinal)..]);
        return document.RootElement.TryGetProperty("Items", out var items) && items.TryGetProperty(itemType, out var values)
            ? values.EnumerateArray()
                    .Select(item => (IReadOnlyDictionary<string, string>)item.EnumerateObject().ToDictionary(z => z.Name, z => z.Value.GetString() ?? ""))
                    .ToArray()
            : [];
    }

    public void Dispose()
    {
        try
        {
            System.IO.Directory.Delete(Directory, recursive: true);
        }
        catch (IOException)
        {
            // Best effort; the temp folder is reaped eventually.
        }
        catch (UnauthorizedAccessException) { }
    }

    private Task VerifyWithFiles(object target, string[] files) =>
        files.Length == 0 ? Verify(target, settings: _settings).ToTask() : Verify(target, FileTargets(files), settings: _settings).ToTask();

    private async Task<ProjectBuildResult> Build(string projectDirectory, string command)
    {
        await RunDotnet($"{command} -bl", projectDirectory);
        var build = BinaryLog.ReadBuild(Path.Combine(projectDirectory, "msbuild.binlog"));
        return new(Path.GetFileName(projectDirectory), ListConfigurationFiles(Path.Combine(projectDirectory, "bin")), ReadWarnings(build));
    }

    private static string[] ReadWarnings(Microsoft.Build.Logging.StructuredLogger.Build build) =>
    [
        .. build.FindChildrenRecursive<Warning>()
                .Select(z => $"{z.Code}: {z.Text}")
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal),
    ];

    /// <summary>
    ///     Each of <paramref name="files" /> (paths relative to the scaffold) as a snapshot of its own, named after the file. In an
    ///     <c>appsettings.schema.json</c>, the SchemaStore schema Optionary embeds becomes a placeholder: it is large, and the
    ///     Optionary.Build.Tasks tests cover it.
    /// </summary>
    private VerifyTests.Target[] FileTargets(IEnumerable<string> files) =>
    [
        .. files.Select(
            file =>
            {
                var text = ReadFile(file);
                if (Path.GetFileName(file) == "appsettings.schema.json"
                 && JsonNode.Parse(text)?["definitions"] is JsonObject definitions
                 && definitions.ContainsKey("SchemaStoreAppSettings"))
                {
                    definitions["SchemaStoreAppSettings"] = "{SchemaStore's appsettings schema}";
                    text = definitions.Root.ToJsonString(
                        new() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }
                    );
                }

                return new VerifyTests.Target("json", text, Path.GetFileNameWithoutExtension(file));
            }
        ),
    ];

    // The one .nupkg dotnet pack wrote under the project directory.
    private static string FindPackage(string projectDirectory)
    {
        var packages = System.IO.Directory.EnumerateFiles(projectDirectory, "*.nupkg", SearchOption.AllDirectories).ToArray();
        return packages.Length == 1
            ? packages[0]
            : throw new TUnitException($"dotnet pack produced {packages.Length} packages for {projectDirectory}, not one.");
    }

    /// <summary>
    ///     A package's identity (from its nuspec) and its files, without the zip package's bookkeeping entries (<c>_rels/</c>,
    ///     <c>package/services/metadata/</c>, <c>[Content_Types].xml</c>) and the nuspec, which no target controls.
    /// </summary>
    private static PackageVerificationResult ReadPackage(string nupkgPath)
    {
        using var archive = ZipFile.OpenRead(nupkgPath);
        var nuspecEntry = archive.Entries.Single(z => !z.FullName.Contains('/', StringComparison.Ordinal) && z.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
        XDocument nuspec;
        using (var stream = nuspecEntry.Open())
        {
            nuspec = XDocument.Load(stream);
        }

        var metadata = nuspec.Root!.Elements().Single(z => z.Name.LocalName == "metadata");
        var files = archive.Entries
                           .Select(z => Uri.UnescapeDataString(z.FullName))
                           .Where(
                                z => !z.StartsWith("_rels/", StringComparison.OrdinalIgnoreCase)
                                 && !z.StartsWith("package/services/metadata/", StringComparison.OrdinalIgnoreCase)
                                 && !z.Equals("[Content_Types].xml", StringComparison.OrdinalIgnoreCase)
                                 && z != nuspecEntry.FullName
                            )
                           .Order(StringComparer.Ordinal)
                           .ToArray();
        return new(
            metadata.Elements().Single(z => z.Name.LocalName == "id").Value,
            metadata.Elements().Single(z => z.Name.LocalName == "version").Value,
            files
        );
    }

    /// <summary>
    ///     The configuration-shaped file names (json/yaml/yml/toml) under <paramref name="directory" />, without the .NET SDK's own
    ///     <c>*.deps.json</c>, <c>*.runtimeconfig.json</c>, <c>*.runtimeconfig.dev.json</c> and <c>*.staticwebassets.endpoints.json</c>.
    /// </summary>
    private static string[] ListConfigurationFiles(string directory) =>
        System.IO.Directory.Exists(directory)
            ?
            [
                .. System.IO.Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                         .Select(Path.GetFileName)
                         .OfType<string>()
                         .Where(
                              z => z.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                               || z.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
                               || z.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
                               || z.EndsWith(".toml", StringComparison.OrdinalIgnoreCase)
                          )
                         .Where(
                              z => !z.EndsWith(".deps.json", StringComparison.OrdinalIgnoreCase)
                               && !z.EndsWith(".runtimeconfig.json", StringComparison.OrdinalIgnoreCase)
                               && !z.EndsWith(".runtimeconfig.dev.json", StringComparison.OrdinalIgnoreCase)
                               && !z.EndsWith(".staticwebassets.endpoints.json", StringComparison.OrdinalIgnoreCase)
                          )
                         .Order(StringComparer.Ordinal),
            ]
            : [];

    private static async Task<string> RunDotnet(string arguments, string workingDirectory, bool rollForward = false)
    {
        var (exitCode, output) = await Dotnet(arguments, workingDirectory, rollForward);
        return exitCode == 0 ? output : throw new TUnitException($"dotnet {arguments} failed for {workingDirectory}:\n{output}");
    }

    /// <summary>
    ///     Runs <c>dotnet</c> in an environment of its own: without CI variables, which would flip <c>ContinuousIntegrationBuild</c>
    ///     and make local and CI runs differ, and without the MSBuild variables this process got from the in-proc MSBuild
    ///     (MSBuild.ProjectCreation) or the build that started it, so the scaffold resolves its own SDK.
    /// </summary>
    private static async Task<(int ExitCode, string Output)> Dotnet(string arguments, string workingDirectory, bool rollForward = false)
    {
        var startInfo = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host ? host : "dotnet", arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var name in startInfo.Environment.Keys.Where(z => z.StartsWith("MSBuild", StringComparison.OrdinalIgnoreCase)).ToArray())
        {
            startInfo.Environment.Remove(name);
        }

        foreach (var name in (string[])["GITHUB_ACTIONS", "CI", "TF_BUILD", "GITLAB_CI", "APPVEYOR", "TEAMCITY_VERSION"])
        {
            startInfo.Environment.Remove(name);
        }

        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.Environment["DOTNET_NOLOGO"] = "1";
        // No MSBuild nodes outliving the test.
        startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        // A net10.0 application needs the .NET 10 runtime; let it run on a newer major one when that's all there is.
        if (rollForward) startInfo.Environment["DOTNET_ROLL_FORWARD"] = "Major";

        using var process = Process.Start(startInfo)!;
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, await standardOutput + await standardError);
    }

    private static string ResolveSymbolicLinks(string path)
    {
        if (OperatingSystem.IsWindows()) return path;

        var resolved = "/";
        foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            resolved = Path.Combine(resolved, segment);
            if (new DirectoryInfo(resolved).ResolveLinkTarget(returnFinalTarget: true) is { } target)
            {
                resolved = target.FullName;
            }
        }

        return resolved;
    }

    private static string ScrubTargetFramework(string line) => Regex.Replace(line, @"\bnet1[01]\.0\b", "{TargetFramework}");
}

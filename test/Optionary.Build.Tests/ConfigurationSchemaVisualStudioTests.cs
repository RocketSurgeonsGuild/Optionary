using Microsoft.Build.Utilities.ProjectCreation;

namespace Optionary.Build.Tests;

/// <summary>
///     Covers <c>Optionary.Schema.VisualStudio.targets</c>: the <c>JsonSchemaSegment</c> items Visual Studio's JSON editor reads. A
///     host gets one for its merged <c>obj/appsettings.schema.json</c>; every project gets one for each referenced package that ships
///     a root <c>ConfigurationSchema.json</c>, unless the package (or the project) declares it already. The items are added by a
///     target, so each test builds just far enough to run it. What Visual Studio does with them can't be tested here.
/// </summary>
[TargetFrameworks]
public class ConfigurationSchemaVisualStudioTests(string targetFramework)
{
    private const string ResolveReferences = "-t:ResolveAssemblyReferences";

    /// <summary>
    ///     What a Visual Studio design-time build runs to get the references, without compiling (its project system's
    ///     CollectResolvedCompilationReferencesDesignTime, which only exists inside Visual Studio, builds on this target).
    /// </summary>
    private const string DesignTimeReferences = "-t:ResolveAssemblyReferencesDesignTime -p:DesignTimeBuild=true -p:BuildingInsideVisualStudio=true";

    private const string FilePathPattern = @"appsettings\..*json";

    private static ProjectCreator HostProject(string targetFramework) => ProjectCreator.Templates.SdkCsproj(targetFramework: targetFramework, outputType: "Exe");

    private static ProjectCreator Library(string targetFramework) => ProjectCreator.Templates.SdkCsproj(targetFramework: targetFramework);

    private static string HostSchemaPath(OptionaryTestProject project, string relativePath) =>
        WithoutPrivateSymlink(Path.GetFullPath(Path.Combine(project.Directory, relativePath, "obj", "appsettings.schema.json")));

    /// <summary>
    ///     A package's root <c>ConfigurationSchema.json</c> in whichever folder NuGet extracted it to; the folder is named by the
    ///     lower-cased ID and the version.
    /// </summary>
    private static bool IsPackageSchema(string identity, PackageVerificationResult package) =>
        identity.EndsWith(
            Path.Join(package.PackageId.ToLowerInvariant(), package.Version.ToLowerInvariant(), "ConfigurationSchema.json"),
            StringComparison.OrdinalIgnoreCase
        );

    // The macOS temp folder is under /var, a symlink to /private/var: compare paths without the prefix, whichever form they have.
    private static string WithoutPrivateSymlink(string path) =>
        OperatingSystem.IsMacOS() && path.StartsWith("/private/", StringComparison.Ordinal) ? path["/private".Length..] : path;

    private static string[] Identities(IReadOnlyList<IReadOnlyDictionary<string, string>> segments) =>
        [.. segments.Select(z => WithoutPrivateSymlink(Path.GetFullPath(z["Identity"]))).Order(StringComparer.Ordinal)];

    private static async Task ShouldAllMatchTheFilePattern(IReadOnlyList<IReadOnlyDictionary<string, string>> segments)
    {
        foreach (var segment in segments)
        {
            await segment["FilePathPattern"].Should().BeEqualTo(FilePathPattern);
        }
    }

    private async Task<(OptionaryTestProject Project, PackageVerificationResult[] Packages)> CreateProjectWithPackedLibraries(params string[] packageIds)
    {
        var project = new OptionaryTestProject();
        var packages = new List<PackageVerificationResult>();
        foreach (var packageId in packageIds)
        {
            var library = Library(targetFramework);
            if (packageId.EndsWith("Own", StringComparison.Ordinal))
            {
                // Declares its own segment from buildTransitive, the way Aspire's client packages do.
                library.ItemInclude(
                    "None",
                    "Own.targets",
                    metadata: new Dictionary<string, string?>
                    {
                        ["Pack"] = "true", ["PackagePath"] = $"buildTransitive/{targetFramework}/{packageId}.targets", ["Visible"] = "false",
                    }
                );
                project.AddFile(
                    $"{packageId}/Own.targets",
                    """
                    <Project>
                      <ItemGroup>
                        <JsonSchemaSegment Include="$(MSBuildThisFileDirectory)..\..\ConfigurationSchema.json" FilePathPattern="appsettings\..*json" />
                      </ItemGroup>
                    </Project>
                    """
                );
            }

            project
               .AddProject($"{packageId}/{packageId}.csproj", library)
               .AddFile($"{packageId}/Fragments.cs", Fragments.Greetings);
            var package = await project.PackToLocalFeed(packageId);
            await package.Files.Should().Contain("ConfigurationSchema.json");
            packages.Add(package);
        }

        return (project, packages.ToArray());
    }

    private static ProjectCreator ReferencePackages(ProjectCreator project, params PackageVerificationResult[] packages)
    {
        foreach (var package in packages)
        {
            project.ItemPackageReference(package.PackageId, package.Version);
        }

        return project;
    }

    [Test]
    public async Task Host_AddsASegmentForItsMergedSchema_AndNothingForProjectReferences()
    {
        using var project = new OptionaryTestProject();
        var lib = Library(targetFramework);
        project.AddProject("lib/lib.csproj", lib, track: false).AddFile("lib/Fragments.cs", Fragments.Greetings);
        project.AddProject("host/host.csproj", HostProject(targetFramework).ItemProjectReference(lib)).AddFile("host/Program.cs", Fragments.Program);

        var segments = await project.EvaluateItems("host", "JsonSchemaSegment", ResolveReferences);

        await Identities(segments).Should().BeEquivalentTo([HostSchemaPath(project, "host")], CollectionOrdering.Matching);
        await ShouldAllMatchTheFilePattern(segments);
    }

    /// <summary>
    ///     The items are there in the build a Visual Studio session runs when it loads the project.
    /// </summary>
    [Test]
    public async Task Host_AddsItsSegmentInDesignTimeBuilds()
    {
        using var project = new OptionaryTestProject();
        project.AddProject("host/host.csproj", HostProject(targetFramework)).AddFile("host/Program.cs", Fragments.Program);

        var segments = await project.EvaluateItems("host", "JsonSchemaSegment", DesignTimeReferences);

        await Identities(segments).Should().BeEquivalentTo([HostSchemaPath(project, "host")], CollectionOrdering.Matching);
    }

    [Test]
    public async Task TestProject_AddsASegmentForItsMergedSchema()
    {
        using var project = new OptionaryTestProject();
        project.AddProject("tests/tests.csproj", Library(targetFramework).Property("IsTestProject", "true"));

        var segments = await project.EvaluateItems("tests", "JsonSchemaSegment", ResolveReferences);

        await Identities(segments).Should().BeEquivalentTo([HostSchemaPath(project, "tests")], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Host_AddsASegmentForEachReferencedPackageWithASchema()
    {
        var (project, packages) = await CreateProjectWithPackedLibraries("OptionaryTestVsSegmentsLib1", "OptionaryTestVsSegmentsLib2");
        using var _ = project;
        project.AddProject("host/host.csproj", ReferencePackages(HostProject(targetFramework), packages)).AddFile("host/Program.cs", Fragments.Program);

        var segments = await project.EvaluateItems("host", "JsonSchemaSegment", ResolveReferences);

        var identities = Identities(segments);
        await identities.Should().Contain(HostSchemaPath(project, "host"));
        foreach (var package in packages)
        {
            await Array.FindAll(identities, z => IsPackageSchema(z, package)).Length.Should().BeEqualTo(1);
        }

        await identities.Length.Should().BeEqualTo(identities.Distinct().Count());
        await ShouldAllMatchTheFilePattern(segments);
        await File.Exists(identities.Single(z => IsPackageSchema(z, packages[0]))).Should().BeTrue();
    }

    [Test]
    public async Task Host_AddsPackageSegmentsInDesignTimeBuilds()
    {
        var (project, packages) = await CreateProjectWithPackedLibraries("OptionaryTestVsDesignTimeLib");
        using var _ = project;
        project.AddProject("host/host.csproj", ReferencePackages(HostProject(targetFramework), packages)).AddFile("host/Program.cs", Fragments.Program);

        var segments = await project.EvaluateItems("host", "JsonSchemaSegment", DesignTimeReferences);

        var identities = Identities(segments);
        await identities.Should().Contain(HostSchemaPath(project, "host"));
        await identities.Count(z => IsPackageSchema(z, packages[0])).Should().BeEqualTo(1);
    }

    /// <summary>
    ///     A library gets the segments of the packages it references, but has no merged file of its own.
    /// </summary>
    [Test]
    public async Task Library_AddsPackageSegments_ButNoHostSegment()
    {
        var (project, packages) = await CreateProjectWithPackedLibraries("OptionaryTestVsLibraryConsumedLib");
        using var _ = project;
        project.AddProject("consumer/consumer.csproj", ReferencePackages(Library(targetFramework), packages));

        var segments = await project.EvaluateItems("consumer", "JsonSchemaSegment", ResolveReferences);

        var identities = Identities(segments);
        await identities.Count(z => IsPackageSchema(z, packages[0])).Should().BeEqualTo(1);
        await identities.Should().NotContain(HostSchemaPath(project, "consumer"));
    }

    /// <summary>
    ///     A package that declares the segment itself, as Aspire's client packages do from buildTransitive, ends up once.
    /// </summary>
    [Test]
    public async Task PackageDeclaringItsOwnSegment_IsNotAddedAgain()
    {
        var (project, packages) = await CreateProjectWithPackedLibraries("OptionaryTestVsDeclaredLibOwn", "OptionaryTestVsDiscoveredLib");
        using var _ = project;
        project.AddProject("host/host.csproj", ReferencePackages(HostProject(targetFramework), packages)).AddFile("host/Program.cs", Fragments.Program);

        // Evaluation alone sees what the package declares...
        var declared = Identities(await project.EvaluateItems("host", "JsonSchemaSegment"));
        await declared.Count(z => IsPackageSchema(z, packages[0])).Should().BeEqualTo(1);
        await declared.Count(z => IsPackageSchema(z, packages[1])).Should().BeEqualTo(0);

        // ...and the build adds only what is missing.
        var segments = await project.EvaluateItems("host", "JsonSchemaSegment", ResolveReferences);
        var identities = Identities(segments);
        await identities.Count(z => IsPackageSchema(z, packages[0])).Should().BeEqualTo(1);
        await identities.Count(z => IsPackageSchema(z, packages[1])).Should().BeEqualTo(1);
        await identities.Length.Should().BeEqualTo(3);
    }

    /// <summary>
    ///     A host that declares the segment for its own merged file keeps just that one.
    /// </summary>
    [Test]
    public async Task HostDeclaringItsOwnSegment_IsNotAddedAgain()
    {
        using var project = new OptionaryTestProject();
        var host = HostProject(targetFramework)
           .ItemInclude(
                "JsonSchemaSegment",
                @"$(MSBuildProjectDirectory)\obj\appsettings.schema.json",
                metadata: new Dictionary<string, string?> { ["FilePathPattern"] = FilePathPattern }
            );
        project.AddProject("host/host.csproj", host).AddFile("host/Program.cs", Fragments.Program);

        var segments = await project.EvaluateItems("host", "JsonSchemaSegment", ResolveReferences);

        await Identities(segments).Should().BeEquivalentTo([HostSchemaPath(project, "host")], CollectionOrdering.Matching);
    }

    [Test]
    public async Task VisualStudioOptOut_AddsNoSegments_ButPackageSegmentsDeclaredByPackagesRemain()
    {
        var (project, packages) = await CreateProjectWithPackedLibraries("OptionaryTestVsOptOutLibOwn", "OptionaryTestVsOptOutDiscoveredLib");
        using var _ = project;
        project.AddProject("host/host.csproj", ReferencePackages(HostProject(targetFramework), packages)).AddFile("host/Program.cs", Fragments.Program);

        var segments = await project.EvaluateItems("host", "JsonSchemaSegment", $"{ResolveReferences} -p:OptionarySchemaVisualStudio=false");

        var identities = Identities(segments);
        await identities.Length.Should().BeEqualTo(1);
        await IsPackageSchema(identities[0], packages[0]).Should().BeTrue();
    }

    [Test]
    public async Task SchemaOptOut_AddsNoSegments_ButPackageSegmentsDeclaredByPackagesRemain()
    {
        var (project, packages) = await CreateProjectWithPackedLibraries("OptionaryTestVsSchemaOffLibOwn", "OptionaryTestVsSchemaOffDiscoveredLib");
        using var _ = project;
        project.AddProject("host/host.csproj", ReferencePackages(HostProject(targetFramework), packages)).AddFile("host/Program.cs", Fragments.Program);

        var segments = await project.EvaluateItems("host", "JsonSchemaSegment", $"{ResolveReferences} -p:OptionarySchema=false");

        var identities = Identities(segments);
        await identities.Length.Should().BeEqualTo(1);
        await IsPackageSchema(identities[0], packages[0]).Should().BeTrue();
    }
}

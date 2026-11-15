using Microsoft.Build.Utilities.ProjectCreation;

namespace Optionary.Build.Tests;

/// <summary>
///     Covers <c>Optionary.Schema.targets</c>: libraries write their own configuration schema fragments to
///     <c>obj/.../ConfigurationSchema.json</c> and pack it at the package root, and hosts merge theirs and every reference's into
///     <c>obj/appsettings.schema.json</c>, copied to the output directory and published. The fragments are hand-written (see
///     <see cref="Fragments" />), so these don't depend on the generator.
/// </summary>
[TargetFrameworks]
public class ConfigurationSchemaTests(string targetFramework)
{
    private const string HostSchema = "host/obj/appsettings.schema.json";

    [Test]
    public async Task Library_WritesItsSectionsToObj_AndPacksThemAtThePackageRoot()
    {
        using var project = new OptionaryTestProject();
        project
           .AddProject("lib/lib.csproj", ProjectCreator.Templates.SdkCsproj(targetFramework: targetFramework))
           .AddFile("lib/Fragments.cs", Fragments.Greetings + "\n" + Fragments.Shared);

        var objSchema = $"lib/obj/Release/{targetFramework}/ConfigurationSchema.json";
        await project.VerifyProjectsAndPackages(objSchema);

        await project.ReadPackageFile("lib", "ConfigurationSchema.json").Should().BeEqualTo(project.ReadFile(objSchema));
    }

    private OptionaryTestProject CreateHostReferencingLibraries()
    {
        var project = new OptionaryTestProject();

        // track: false - built only through the host's ProjectReference.
        var lib1 = ProjectCreator.Templates.SdkCsproj(targetFramework: targetFramework);
        project.AddProject("lib1/lib1.csproj", lib1, track: false).AddFile("lib1/Fragments.cs", Fragments.Greetings);
        var lib2 = ProjectCreator.Templates.SdkCsproj(targetFramework: targetFramework);
        project.AddProject("lib2/lib2.csproj", lib2, track: false).AddFile("lib2/Fragments.cs", Fragments.Shared);

        var host = ProjectCreator.Templates.SdkCsproj(targetFramework: targetFramework, outputType: "Exe").ItemProjectReference(lib1).ItemProjectReference(lib2);
        project
           .AddProject("host/host.csproj", host)
           .AddFile("host/Program.cs", Fragments.Program)
           .AddFile("host/Fragments.cs", Fragments.App);
        return project;
    }

    [Test]
    public async Task HostReferencingLibraries_MergesTheirSections_AndCopiesTheSchemaToItsOutput()
    {
        using var project = CreateHostReferencingLibraries();

        await project.VerifyProjects(HostSchema);

        await project.ReadFile($"host/bin/Debug/{targetFramework}/appsettings.schema.json").Should().BeEqualTo(project.ReadFile(HostSchema));
    }

    [Test]
    public async Task HostReferencingLibraries_PublishesTheSchema()
    {
        using var project = CreateHostReferencingLibraries();

        await project.VerifyPublish("host");

        await project.ReadFile($"host/bin/Release/{targetFramework}/publish/appsettings.schema.json").Should().BeEqualTo(project.ReadFile(HostSchema));
    }

    private async Task<OptionaryTestProject> CreateHostReferencingPackages()
    {
        var project = new OptionaryTestProject();
        project
           .AddProject("lib1/OptionaryTestSchemaLib1.csproj", ProjectCreator.Templates.SdkCsproj(targetFramework: targetFramework))
           .AddFile("lib1/Fragments.cs", Fragments.Greetings);
        project
           .AddProject("lib2/OptionaryTestSchemaLib2.csproj", ProjectCreator.Templates.SdkCsproj(targetFramework: targetFramework))
           .AddFile("lib2/Fragments.cs", Fragments.Shared);
        var lib1 = await project.PackToLocalFeed("lib1");
        var lib2 = await project.PackToLocalFeed("lib2");
        await lib1.Files.Should().Contain("ConfigurationSchema.json");
        await lib2.Files.Should().Contain("ConfigurationSchema.json");

        var host = ProjectCreator
                  .Templates.SdkCsproj(targetFramework: targetFramework, outputType: "Exe")
                  .ItemPackageReference(lib1.PackageId, lib1.Version)
                  .ItemPackageReference(lib2.PackageId, lib2.Version);
        project
           .AddProject("host/host.csproj", host)
           .AddFile("host/Program.cs", Fragments.Program)
           .AddFile("host/Fragments.cs", Fragments.App);
        return project;
    }

    [Test]
    public async Task HostReferencingPackages_MergesTheirSections_AndCopiesTheSchemaToItsOutput()
    {
        using var project = await CreateHostReferencingPackages();

        await project.VerifyProjects(HostSchema);

        await project.ReadFile($"host/bin/Debug/{targetFramework}/appsettings.schema.json").Should().BeEqualTo(project.ReadFile(HostSchema));
    }

    [Test]
    public async Task HostReferencingPackages_PublishesTheSchema()
    {
        using var project = await CreateHostReferencingPackages();

        await project.VerifyPublish("host");

        await project.ReadFile($"host/bin/Release/{targetFramework}/publish/appsettings.schema.json").Should().BeEqualTo(project.ReadFile(HostSchema));
    }

    /// <summary>
    ///     Test projects are hosts too, whatever their output type: they read configuration the way the application does.
    /// </summary>
    [Test]
    public async Task TestProject_MergesTheSectionsOfWhatItTests()
    {
        using var project = new OptionaryTestProject();
        var lib = ProjectCreator.Templates.SdkCsproj(targetFramework: targetFramework);
        project.AddProject("lib/lib.csproj", lib, track: false).AddFile("lib/Fragments.cs", Fragments.Greetings);
        project.AddProject(
            "tests/tests.csproj",
            ProjectCreator.Templates.SdkCsproj(targetFramework: targetFramework).Property("IsTestProject", "true").ItemProjectReference(lib)
        );

        await project.BuildProject("tests");

        await project.Warnings("tests").Should().BeEmpty();
        await project.ReadFile("tests/obj/appsettings.schema.json").Should().Contain("Unknown key in section \\\"Greetings\\\"");
        await project.ReadFile($"tests/bin/Debug/{targetFramework}/appsettings.schema.json").Should().BeEqualTo(project.ReadFile("tests/obj/appsettings.schema.json"));
    }

    /// <summary>
    ///     <c>OptionarySchemaCopyToOutput=false</c>, which a host that embeds its configuration (MAUI, Uno) sets: the schema is still
    ///     merged into <c>obj/</c>, but neither copied to the output directory nor published.
    /// </summary>
    [Test]
    public async Task CopyToOutputOff_KeepsTheSchemaInObj_AndNeitherCopiesNorPublishesIt()
    {
        using var project = new OptionaryTestProject();
        project
           .AddProject(
                "host/host.csproj",
                ProjectCreator.Templates.SdkCsproj(targetFramework: targetFramework, outputType: "Exe").Property("OptionarySchemaCopyToOutput", "false")
            )
           .AddFile("host/Program.cs", Fragments.Program)
           .AddFile("host/Fragments.cs", Fragments.App);

        await project.BuildProject("host");

        await project.Warnings("host").Should().BeEmpty();
        await project.ReadFile(HostSchema).Should().Contain("Unknown key in section \\\"App\\\"");
        await project.Exists($"host/bin/Debug/{targetFramework}/appsettings.schema.json").Should().BeFalse();

        var published = await project.Publish("host");

        await published.Should().NotContain("appsettings.schema.json");
        await project.Exists($"host/bin/Release/{targetFramework}/publish/appsettings.schema.json").Should().BeFalse();
    }

    /// <summary>
    ///     <c>OptionarySchemaHost=true</c> makes a library a host, which a host framework (Dovetail's SDK, for one) sets from its own
    ///     application detection: it merges <c>obj/appsettings.schema.json</c> and copies it, instead of writing a library file.
    /// </summary>
    [Test]
    public async Task HostOverride_TurnsALibraryIntoAHost()
    {
        using var project = new OptionaryTestProject();
        project
           .AddProject("lib/lib.csproj", ProjectCreator.Templates.SdkCsproj(targetFramework: targetFramework).Property("OptionarySchemaHost", "true"))
           .AddFile("lib/Fragments.cs", Fragments.Greetings);

        await project.BuildProject("lib");

        await project.Warnings("lib").Should().BeEmpty();
        await project.ReadFile("lib/obj/appsettings.schema.json").Should().Contain("Unknown key in section \\\"Greetings\\\"");
        await project.ReadFile($"lib/bin/Debug/{targetFramework}/appsettings.schema.json").Should().BeEqualTo(project.ReadFile("lib/obj/appsettings.schema.json"));
        await project.Exists($"lib/obj/Debug/{targetFramework}/ConfigurationSchema.json").Should().BeFalse();
    }

    /// <summary>
    ///     <c>OptionarySchemaHost=false</c> makes an application a library: it writes its own fragments, and no host file.
    /// </summary>
    [Test]
    public async Task HostOverride_TurnsAnApplicationIntoALibrary()
    {
        using var project = new OptionaryTestProject();
        project
           .AddProject(
                "host/host.csproj",
                ProjectCreator.Templates.SdkCsproj(targetFramework: targetFramework, outputType: "Exe").Property("OptionarySchemaHost", "false")
            )
           .AddFile("host/Program.cs", Fragments.Program)
           .AddFile("host/Fragments.cs", Fragments.App);

        await project.BuildProject("host");

        await project.Warnings("host").Should().BeEmpty();
        await project.ReadFile($"host/obj/Debug/{targetFramework}/ConfigurationSchema.json").Should().Contain("\"App\"");
        await project.Exists(HostSchema).Should().BeFalse();
        await project.Exists($"host/bin/Debug/{targetFramework}/appsettings.schema.json").Should().BeFalse();
    }
}

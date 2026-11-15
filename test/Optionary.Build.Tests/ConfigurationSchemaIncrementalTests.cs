using Microsoft.Build.Utilities.ProjectCreation;

namespace Optionary.Build.Tests;

/// <summary>
///     The schema across builds: an up-to-date one isn't merged again (or touched), and the files are deleted once a project stops
///     writing them - when the schema is turned off, or a library no longer declares any section.
/// </summary>
public class ConfigurationSchemaIncrementalTests
{
    private const string TargetFramework = "net10.0";

    [Test]
    public async Task Host_SkipsAnUpToDateSchema_AndDeletesItWhenTurnedOff()
    {
        using var project = new OptionaryTestProject();
        project
           .AddProject("host/host.csproj", ProjectCreator.Templates.SdkCsproj(targetFramework: TargetFramework, outputType: "Exe"))
           .AddFile("host/Program.cs", Fragments.Program)
           .AddFile("host/Fragments.cs", Fragments.App);
        var schema = project.FullPath("host/obj/appsettings.schema.json");
        var output = project.FullPath($"host/bin/Debug/{TargetFramework}/appsettings.schema.json");

        await Merges(await project.BuildProject("host")).Should().BeTrue();
        var written = File.GetLastWriteTimeUtc(schema);
        await File.Exists(output).Should().BeTrue();

        await Merges(await project.BuildProject("host")).Should().BeFalse();
        await File.GetLastWriteTimeUtc(schema).Should().BeEqualTo(written);

        await project.BuildProject("host", "-p:OptionarySchema=false");
        await File.Exists(schema).Should().BeFalse();
        await File.Exists(output).Should().BeFalse();

        await Merges(await project.BuildProject("host")).Should().BeTrue();
        await File.Exists(schema).Should().BeTrue();
        await File.Exists(output).Should().BeTrue();
    }

    [Test]
    public async Task Library_DeletesItsSchema_WhenItNoLongerDeclaresSections()
    {
        using var project = new OptionaryTestProject();
        project
           .AddProject("lib/lib.csproj", ProjectCreator.Templates.SdkCsproj(targetFramework: TargetFramework))
           .AddFile("lib/Fragments.cs", Fragments.Greetings);
        var schema = project.FullPath($"lib/obj/Debug/{TargetFramework}/ConfigurationSchema.json");

        await project.BuildProject("lib");
        await File.Exists(schema).Should().BeTrue();

        File.Delete(project.FullPath("lib/Fragments.cs"));
        project.AddFile("lib/Class1.cs", "namespace Lib { public class Class1 { } }");
        await project.BuildProject("lib");
        await File.Exists(schema).Should().BeFalse();
    }

    private static bool Merges(Microsoft.Build.Logging.StructuredLogger.Build build) =>
        build.FindChildrenRecursive<Microsoft.Build.Logging.StructuredLogger.Task>(z => z.Name == "MergeConfigurationSchema").Count > 0;
}

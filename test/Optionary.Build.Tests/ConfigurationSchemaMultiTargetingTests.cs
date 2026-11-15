using Microsoft.Build.Utilities.ProjectCreation;

namespace Optionary.Build.Tests;

/// <summary>
///     Projects that target both frameworks: each target framework reads its own assembly, so a library packs only the first one's
///     ConfigurationSchema.json (a second root file would be NU5118), and a host writes the stable obj/appsettings.schema.json
///     from its first target framework only, so the two inner builds never write the same file.
/// </summary>
public class ConfigurationSchemaMultiTargetingTests
{
    [Test]
    public async Task Library_PacksTheSchemaOfItsFirstTargetFramework()
    {
        using var project = new OptionaryTestProject();
        project
           .AddProject("lib/OptionaryTestMultiTargeted.csproj", ProjectCreator.Templates.SdkCsproj(targetFrameworks: ["net10.0", "net11.0"]))
           .AddFile("lib/Fragments.cs", Fragments.PerFramework);

        var package = await project.PackToLocalFeed("lib");

        await package.Files.Count(z => z == "ConfigurationSchema.json").Should().BeEqualTo(1);
        await project.ReadFile("lib/obj/Release/net10.0/ConfigurationSchema.json").Should().Contain("Net10");
        await project.ReadFile("lib/obj/Release/net11.0/ConfigurationSchema.json").Should().Contain("Net11");
        await project.ReadPackageFile("lib", "ConfigurationSchema.json").Should().BeEqualTo(project.ReadFile("lib/obj/Release/net10.0/ConfigurationSchema.json"));
        await project.Warnings("lib").Should().BeEmpty();
    }

    [Test]
    public async Task Host_WritesTheStableSchemaFromItsFirstTargetFramework()
    {
        using var project = new OptionaryTestProject();
        project
           .AddProject("host/host.csproj", ProjectCreator.Templates.SdkCsproj(targetFrameworks: ["net10.0", "net11.0"], outputType: "Exe"))
           .AddFile("host/Program.cs", Fragments.Program)
           .AddFile("host/Fragments.cs", Fragments.PerFramework);

        await project.BuildProject("host");

        await project.Warnings("host").Should().BeEmpty();
        await project.ReadFile("host/obj/appsettings.schema.json").Should().Contain("Net10");
        await project.ReadFile("host/obj/Debug/net11.0/appsettings.schema.json").Should().Contain("Net11");
        await project.Exists("host/obj/Debug/net10.0/appsettings.schema.json").Should().BeFalse();
        await project.ReadFile("host/bin/Debug/net10.0/appsettings.schema.json").Should().BeEqualTo(project.ReadFile("host/obj/appsettings.schema.json"));
        await project.ReadFile("host/bin/Debug/net11.0/appsettings.schema.json").Should().BeEqualTo(project.ReadFile("host/obj/Debug/net11.0/appsettings.schema.json"));
    }
}

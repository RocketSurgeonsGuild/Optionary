using Microsoft.Build.Utilities.ProjectCreation;

namespace Optionary.Build.Tests;

/// <summary>
///     OPT1001 (<c>_OptionaryCheckGenerator</c> in <c>Optionary.targets</c>): a project that references Optionary.dll without
///     Optionary's source generator fails to build with an error that says how to fix it, instead of the compiler errors a host
///     framework's call to the missing registration method would give.
/// </summary>
public class GeneratorCheckTests
{
    private const string TargetFramework = "net10.0";

    private const string Program = """
        using app;
        using Microsoft.Extensions.DependencyInjection;

        new ServiceCollection().AddOptionaryConfigurationOptions();
        System.Console.WriteLine("registered");
        """;

    /// <summary>
    ///     The generator is gone from <c>@(Analyzer)</c>, the way a project turns a package's source generator off: OPT1001 stops the
    ///     build before the compiler reports the missing registration method.
    /// </summary>
    [Test]
    public async Task WithoutTheGenerator_FailsWithOPT1001()
    {
        using var project = new OptionaryTestProject();
        project
           .AddProject("app/app.csproj", ProjectCreator.Templates.SdkCsproj(targetFramework: TargetFramework, outputType: "Exe"))
           .AddFile("app/Program.cs", Program)
           .AddFile(
                "Directory.Build.targets",
                """
                <Project>
                  <Target Name="RemoveOptionaryGenerator" AfterTargets="ResolveLockFileAnalyzers">
                    <ItemGroup>
                      <Analyzer Remove="@(Analyzer)" Condition="'%(Filename)' == 'Optionary.Analyzers'" />
                    </ItemGroup>
                  </Target>
                </Project>
                """
            );

        var output = await project.BuildProjectExpectingFailure("app");

        await output.Should().Contain("error OPT1001: app references Optionary.dll, but Optionary's source generator (Optionary.Analyzers) isn't loaded");
        await output.Should().NotContain("error CS");
    }

    /// <summary>
    ///     <c>ExcludeAssets="analyzers"</c> doesn't keep a package's analyzers from the compiler: the .NET SDK (10.0.401 and
    ///     11.0.100) passes every analyzer in the package's <c>analyzers/</c> folder whatever the reference excludes. The generator
    ///     still runs, so there is nothing for OPT1001 to report. Should the SDK start honouring the exclusion, this fails with OPT1001,
    ///     which is then the behaviour section 4.4 of the migration plan describes.
    /// </summary>
    [Test]
    public async Task ExcludingTheAnalyzersAsset_StillLoadsTheGenerator()
    {
        using var project = new OptionaryTestProject(referenceOptionary: false);
        project
           .AddProject(
                "app/app.csproj",
                OptionaryTestProject.ReferenceOptionary(
                    ProjectCreator.Templates.SdkCsproj(targetFramework: TargetFramework, outputType: "Exe"),
                    new Dictionary<string, string?> { ["ExcludeAssets"] = "analyzers" }
                )
            )
           .AddFile("app/Program.cs", Program);

        await project.BuildProject("app");

        await project.Warnings("app").Should().BeEmpty();
        await ( await project.Run("app") ).Should().Contain("registered");
    }

    [Test]
    public async Task WithTheGenerator_Builds()
    {
        using var project = new OptionaryTestProject();
        project
           .AddProject("app/app.csproj", ProjectCreator.Templates.SdkCsproj(targetFramework: TargetFramework, outputType: "Exe"))
           .AddFile("app/Program.cs", Program);

        await project.BuildProject("app");

        await project.Warnings("app").Should().BeEmpty();
    }
}

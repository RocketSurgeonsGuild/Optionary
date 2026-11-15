using Microsoft.Build.Utilities.ProjectCreation;

namespace Optionary.Build.Tests;

/// <summary>
///     <c>[assembly: Optionary&lt;T&gt;("Section")]</c> describes a type from a package with that package's XML docs. The compiler
///     loads no docs for references and analyzers can't read files, so <c>Optionary.Schema.targets</c> passes the doc file next to
///     each package reference to the generator as an <c>AdditionalFiles</c> item. This checks that wiring in a real build.
/// </summary>
[TargetFrameworks]
public class ConfigurationSchemaFragmentTests(string targetFramework)
{
    [Test]
    public async Task PackageType_IsDescribedWithItsXmlDocs()
    {
        using var project = new OptionaryTestProject();
        project
           .AddProject(
                "app/app.csproj",
                ProjectCreator
                   .Templates.SdkCsproj(targetFramework: targetFramework, outputType: "Exe")
                   .Property("EmitCompilerGeneratedFiles", "true")
                   // HostOptions comes from the generic host's own package.
                   .ItemPackageReference("Microsoft.Extensions.Hosting", OptionaryPackage.CentralPackageVersion("Microsoft.Extensions.Hosting", targetFramework))
            )
           .AddFile(
                "app/Program.cs",
                """
                [assembly: Optionary.Optionary<Microsoft.Extensions.Hosting.HostOptions>("Host")]

                System.Console.WriteLine("hello");
                """
            );

        await project.Run("app");

        var fragment = File.ReadAllText(
            Directory.EnumerateFiles(project.FullPath("app/obj"), "Optionary.Schema.g.cs", SearchOption.AllDirectories).Single()
        );
        await fragment.Should().Contain("Optionary.Schema.v1:Host");
        // HostOptions.ShutdownTimeout's <summary>, from Microsoft.Extensions.Hosting.xml in the package.
        await fragment.Should().Contain("Gets or sets the default timeout for IHost.StopAsync.");
    }
}

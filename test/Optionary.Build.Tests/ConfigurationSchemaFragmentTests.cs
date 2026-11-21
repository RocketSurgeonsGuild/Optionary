using Microsoft.Build.Utilities.ProjectCreation;

namespace Optionary.Build.Tests;

/// <summary>
///     <c>[assembly: Optionary&lt;T&gt;("Section")]</c> describes a type from a package with that package's XML docs. The compiler
///     loads no docs for references and analyzers can't read files, so <c>Optionary.Schema.targets</c> passes the doc file next to
///     each package reference to the generator as an <c>AdditionalFiles</c> item, under a name that isn't <c>.xml</c>. This checks
///     that wiring in a real build.
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

    /// <summary>
    ///     ASP.NET Core's OpenAPI generator reads every <c>.xml</c> additional file as documentation and writes its comments into
    ///     the compilation as string literals. Handed the documentation of every referenced package, it fails with CS8103 in a
    ///     project with enough of them, so it must see none of what Optionary passes to its own generator.
    /// </summary>
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task PackageDocumentation_StaysOutOfOpenApisGenerator(bool referenceOptionaryDirectly)
    {
        const string summary = "How long the documented library waits.";
        using var project = new OptionaryTestProject(referenceOptionary: false);
        project
           .AddProject(
                "lib/OptionaryTestDocumented.csproj",
                OptionaryTestProject.ReferenceOptionary(ProjectCreator.Templates.SdkCsproj(targetFramework: targetFramework).Property("GenerateDocumentationFile", "true"))
            )
           .AddFile(
                "lib/DocumentedOptions.cs",
                $$"""
                namespace OptionaryTestDocumented;

                /// <summary>Settings of the documented library.</summary>
                public class DocumentedOptions
                {
                    /// <summary>{{summary}}</summary>
                    public int Timeout { get; set; }
                }
                """
            );
        var library = await project.PackToLocalFeed("lib");

        // Without its own reference, the application gets Optionary's build assets through the library's package.
        var app = ProjectCreator
                 .Templates.SdkCsproj(sdk: "Microsoft.NET.Sdk.Web", targetFramework: targetFramework)
                 .Property("EmitCompilerGeneratedFiles", "true")
                 .ItemPackageReference("Microsoft.AspNetCore.OpenApi", OptionaryPackage.CentralPackageVersion("Microsoft.AspNetCore.OpenApi", targetFramework))
                 .ItemPackageReference(library.PackageId, library.Version);
        project
           .AddProject("app/app.csproj", referenceOptionaryDirectly ? OptionaryTestProject.ReferenceOptionary(app) : app)
           .AddFile(
                "app/Program.cs",
                """
                [assembly: Optionary.Optionary<OptionaryTestDocumented.DocumentedOptions>("Documented")]

                var builder = WebApplication.CreateBuilder(args);
                builder.Services.AddOpenApi();
                var app = builder.Build();
                app.MapOpenApi();
                app.MapGet("/", () => "hello");
                app.Run();
                """
            );

        await project.BuildProject("app");

        var generated = Directory
                       .EnumerateFiles(project.FullPath("app/obj"), "*.cs", SearchOption.AllDirectories)
                       .Where(z => File.ReadAllText(z).Contains(summary, StringComparison.Ordinal))
                       .Select(Path.GetFileName)
                       .ToArray();
        await generated.Should().BeEquivalentTo(["Optionary.Schema.g.cs"]);
        // The OpenAPI generator did run, so the line above says something about it.
        await Directory.EnumerateFiles(project.FullPath("app/obj"), "OpenApiXmlCommentSupport.generated.cs", SearchOption.AllDirectories).Should().NotBeEmpty();
    }
}

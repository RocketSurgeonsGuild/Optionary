using System.Text.Json.Nodes;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Optionary.Build.Tests;

/// <summary>
///     The <c>OptionarySchemaFragment</c> item (<c>Optionary.Schema.targets</c>): <c>Include</c> is a section path, and the schema
///     of its value, inline (<c>Schema</c>) or in a JSON file (<c>File</c>), becomes an
///     <c>[assembly: AssemblyMetadata("Optionary.Schema.v1:&lt;section&gt;", "&lt;schema&gt;")]</c> attribute. It then reaches the
///     library schema, the package and every host that references the project, like a generated fragment.
/// </summary>
[TargetFrameworks]
public class SchemaFragmentItemTests(string targetFramework)
{
    private const string HostSchema = "host/obj/appsettings.schema.json";

    /// <summary>
    ///     A fragment file in the v1 format, spread over several lines, with every character MSBuild or C# treats specially in an
    ///     item, a property function or a string literal: <c>; % $ @ ' " \</c>, <c>$(...)</c>, <c>@(...)</c>, <c>%(...)</c>,
    ///     <c>%3B</c> and non-ASCII text.
    /// </summary>
    internal const string Petstore = """
        {
          "type": "object",
          "title": "HttpClients:Petstore",
          "description": "Petstore client; 100% \"generated\" for $(Configuration), @(Compile) and %(Identity) at user@example.com, naïve 'quotes' \\ and %3B.\nSecond line.",
          "properties": {
            "BaseAddress": { "type": "string", "format": "uri", "pattern": "^https?://\\S+$" },
            "Retry": { "type": "object", "allOf": [ { "$ref": "#/definitions/Retry" } ] }
          },
          "definitions": {
            "Retry": { "properties": { "Count": { "type": "integer", "minimum": 0 } } }
          }
        }
        """;

    internal static readonly Dictionary<string, string?> PetstoreFile = new() { ["File"] = "petstore.schema.json" };

    private ProjectCreator Library() =>
        ProjectCreator.Templates.SdkCsproj(targetFramework: targetFramework).ItemInclude("OptionarySchemaFragment", "HttpClients:Petstore", metadata: PetstoreFile);

    [Test]
    public async Task Library_PacksTheFragmentInItsConfigurationSchema()
    {
        using var project = new OptionaryTestProject();
        project.AddProject("lib/lib.csproj", Library()).AddFile("lib/petstore.schema.json", Petstore);

        var objSchema = $"lib/obj/Release/{targetFramework}/ConfigurationSchema.json";
        await project.VerifyProjectsAndPackages(objSchema);

        await project.ReadPackageFile("lib", "ConfigurationSchema.json").Should().BeEqualTo(project.ReadFile(objSchema));
    }

    [Test]
    public async Task HostReferencingTheLibraryProject_MergesTheSection()
    {
        using var project = new OptionaryTestProject();
        var lib = Library();
        project.AddProject("lib/lib.csproj", lib, track: false).AddFile("lib/petstore.schema.json", Petstore);
        project
           .AddProject("host/host.csproj", ProjectCreator.Templates.SdkCsproj(targetFramework: targetFramework, outputType: "Exe").ItemProjectReference(lib))
           .AddFile("host/Program.cs", Fragments.Program)
           .AddFile("host/Fragments.cs", Fragments.App);

        await project.VerifyProjects(HostSchema);
    }

    [Test]
    public async Task HostReferencingTheLibraryPackage_MergesTheSection()
    {
        using var project = new OptionaryTestProject();
        project.AddProject("lib/OptionaryTestPetstore.csproj", Library(), track: false).AddFile("lib/petstore.schema.json", Petstore);
        var lib = await project.PackToLocalFeed("lib");

        project
           .AddProject(
                "host/host.csproj",
                ProjectCreator.Templates.SdkCsproj(targetFramework: targetFramework, outputType: "Exe").ItemPackageReference(lib.PackageId, lib.Version)
            )
           .AddFile("host/Program.cs", Fragments.Program);

        await project.BuildProject("host");

        await PetstoreDescription(project.ReadFile(HostSchema)).Should().BeEqualTo(PetstoreDescription(Petstore));
        await project.Warnings("host").Should().BeEmpty();
    }

    /// <summary>
    ///     A package (an HTTP-client generator, say) whose <c>buildTransitive</c> targets add the item with a file it packs, so the
    ///     project consuming it declares the section it configures.
    /// </summary>
    [Test]
    public async Task PackageWhoseTargetsAddTheItem_ContributesTheSectionToItsConsumer()
    {
        using var project = new OptionaryTestProject();
        project
           .AddProject("clients/OptionaryTestHttpClients.csproj", PackageWithBuildTransitive(), track: false)
           .AddFile("clients/buildTransitive/petstore.schema.json", Petstore)
           .AddFile(
                "clients/buildTransitive/OptionaryTestHttpClients.targets",
                """
                <Project>
                  <ItemGroup>
                    <OptionarySchemaFragment Include="HttpClients:Petstore" File="$(MSBuildThisFileDirectory)petstore.schema.json" />
                  </ItemGroup>
                </Project>
                """
            );
        var clients = await project.PackToLocalFeed("clients");
        await clients.Files.Should().Contain("buildTransitive/OptionaryTestHttpClients.targets");
        // The package's own build doesn't import its buildTransitive targets, so it declares nothing itself.
        await clients.Files.Should().NotContain("ConfigurationSchema.json");

        project
           .AddProject(
                "host/host.csproj",
                ProjectCreator.Templates.SdkCsproj(targetFramework: targetFramework, outputType: "Exe").ItemPackageReference(clients.PackageId, clients.Version)
            )
           .AddFile("host/Program.cs", Fragments.Program);

        await project.BuildProject("host");

        await PetstoreDescription(project.ReadFile(HostSchema)).Should().BeEqualTo(PetstoreDescription(Petstore));
        await project.Warnings("host").Should().BeEmpty();
    }

    /// <summary>
    ///     The shape of an OpenAPI client generator's targets: one item per client, a transform of the generator's own items, with
    ///     the schema inline. Its descriptions carry the characters MSBuild treats specially in metadata (<c>; % $ @</c>) and both
    ///     kinds of quotes. A second item for one of the sections, from the project itself, is merged with it.
    /// </summary>
    [Test]
    public async Task PackageWhoseTargetsTransformItsOwnItems_DeclaresASectionPerItem()
    {
        using var project = new OptionaryTestProject();
        project
           .AddProject("clients/OptionaryTestOpenApi.csproj", PackageWithBuildTransitive(), track: false)
           .AddFile(
                "clients/buildTransitive/OptionaryTestOpenApi.targets",
                """
                <Project>
                  <ItemGroup>
                    <OptionarySchemaFragment
                      Include="@(OpenApiClient->'Clients:%(ClientName)')"
                      Schema='{"type":"object","properties":{"BaseAddress":{"type":"string","description":"Base address; 100% required, costs $5 @ most, the \"real\" one, not the &apos;test&apos; one."}}}' />
                  </ItemGroup>
                </Project>
                """
            );
        var clients = await project.PackToLocalFeed("clients");

        project
           .AddProject(
                "host/host.csproj",
                ProjectCreator
                   .Templates.SdkCsproj(targetFramework: targetFramework, outputType: "Exe")
                   .ItemPackageReference(clients.PackageId, clients.Version)
                   .ItemInclude("OpenApiClient", "petstore.yaml", metadata: new Dictionary<string, string?> { ["ClientName"] = "Petstore" })
                   .ItemInclude("OpenApiClient", "weather.yaml", metadata: new Dictionary<string, string?> { ["ClientName"] = "Weather" })
                   .ItemInclude(
                        "OptionarySchemaFragment",
                        "Clients:Weather",
                        metadata: new Dictionary<string, string?> { ["Schema"] = """{"type":"object","properties":{"Timeout":{"type":"integer"}}}""" }
                    )
            )
           .AddFile("host/Program.cs", Fragments.Program);

        await project.VerifyProjects(HostSchema);
    }

    /// <summary>
    ///     Without the SDK's assembly-info generation, Optionary writes the attribute in a file of its own.
    /// </summary>
    [Test]
    [Arguments("GenerateAssemblyInfo")]
    [Arguments("GenerateAssemblyMetadataAttributes")]
    public async Task Library_WithoutGeneratedMetadataAttributes_StillDeclaresTheSection(string property)
    {
        using var project = new OptionaryTestProject();
        project.AddProject("lib/lib.csproj", Library().Property(property, "false")).AddFile("lib/petstore.schema.json", Petstore);

        await project.BuildProject("lib");

        var schema = project.ReadFile($"lib/obj/Debug/{targetFramework}/ConfigurationSchema.json");
        await PetstoreDescription(schema).Should().BeEqualTo(PetstoreDescription(Petstore));
        await project.Warnings("lib").Should().BeEmpty();
    }

    /// <summary>
    ///     <c>OptionarySchema=false</c> in the library: its assembly doesn't carry the fragment, so a host referencing it doesn't
    ///     see the section.
    /// </summary>
    [Test]
    public async Task Library_WithTheSchemaOff_DoesNotDeclareTheSection()
    {
        using var project = new OptionaryTestProject();
        var lib = Library().Property("OptionarySchema", "false");
        project.AddProject("lib/lib.csproj", lib, track: false).AddFile("lib/petstore.schema.json", Petstore);
        project
           .AddProject("host/host.csproj", ProjectCreator.Templates.SdkCsproj(targetFramework: targetFramework, outputType: "Exe").ItemProjectReference(lib))
           .AddFile("host/Program.cs", Fragments.Program)
           .AddFile("host/Fragments.cs", Fragments.App);

        await project.BuildProject("host");

        await project.ReadFile(HostSchema).Should().NotContain("Petstore");
    }

    private ProjectCreator PackageWithBuildTransitive() =>
        ProjectCreator
           .Templates.SdkCsproj(targetFramework: targetFramework)
           .ItemInclude("None", "buildTransitive/**", metadata: new Dictionary<string, string?> { ["Pack"] = "true", ["PackagePath"] = "buildTransitive/" });

    /// <summary>
    ///     The text of the Petstore section's description, wherever the document puts it.
    /// </summary>
    internal static string? PetstoreDescription(string json) => FindDescription(JsonNode.Parse(json), "Petstore client;");

    private static string? FindDescription(JsonNode? node, string prefix) =>
        node switch
        {
            JsonObject value when value["description"] is JsonValue description
                               && description.TryGetValue<string>(out var text)
                               && text.StartsWith(prefix, StringComparison.Ordinal) => text,
            JsonObject value => value.Select(z => FindDescription(z.Value, prefix)).FirstOrDefault(z => z is not null),
            JsonArray array => array.Select(z => FindDescription(z, prefix)).FirstOrDefault(z => z is not null),
            _ => null,
        };
}

/// <summary>
///     <c>OptionarySchemaFragment</c> items the build rejects, and rebuilding when the file changes.
/// </summary>
public class SchemaFragmentItemErrorTests
{
    private const string TargetFramework = "net10.0";
    private const string ValidSchema = """{"type":"object"}""";

    private static async Task<string> BuildWithItem(string section, Dictionary<string, string?> metadata, string? file = null)
    {
        using var project = new OptionaryTestProject();
        project.AddProject(
            "lib/lib.csproj",
            ProjectCreator.Templates.SdkCsproj(targetFramework: TargetFramework).ItemInclude("OptionarySchemaFragment", section, metadata: metadata)
        );
        if (file is not null) project.AddFile("lib/fragment.schema.json", file);

        return await project.BuildProjectExpectingFailure("lib");
    }

    [Test]
    public async Task NeitherSchemaNorFile_FailsWithOPT1002()
    {
        var output = await BuildWithItem("HttpClients:Petstore", []);

        await output.Should().Contain(
            "error OPT1002: The OptionarySchemaFragment item \"HttpClients:Petstore\" has neither Schema nor File metadata: set exactly one"
        );
    }

    [Test]
    public async Task BothSchemaAndFile_FailsWithOPT1002()
    {
        var output = await BuildWithItem("HttpClients:Petstore", new() { ["Schema"] = ValidSchema, ["File"] = "fragment.schema.json" }, ValidSchema);

        await output.Should().Contain("error OPT1002: The OptionarySchemaFragment item \"HttpClients:Petstore\" has both Schema and File metadata: set exactly one");
    }

    [Test]
    [Arguments("HttpClients::Petstore")]
    [Arguments("HttpClients: :Petstore")]
    [Arguments("HttpClients:")]
    [Arguments(":Petstore")]
    public async Task InvalidSection_FailsWithOPT1003(string section)
    {
        var output = await BuildWithItem(section, new() { ["Schema"] = ValidSchema });

        await output.Should().Contain(
            $"error OPT1003: The OptionarySchemaFragment item \"{section}\" isn't a valid section path: section paths can't have empty segments."
        );
    }

    [Test]
    public async Task MissingFile_FailsWithOPT1004()
    {
        var output = await BuildWithItem("HttpClients:Petstore", new() { ["File"] = "fragment.schema.json" });

        await output.Should().Contain("error OPT1004: The OptionarySchemaFragment item \"HttpClients:Petstore\" has the file \"");
        await output.Should().Contain("lib/fragment.schema.json\", which doesn't exist.");
    }

    [Test]
    [Arguments("[1, 2]", "the schema is not a JSON object")]
    [Arguments("true", "the schema is not a JSON object")]
    [Arguments("{ \"type\": ", "the schema is not valid JSON")]
    [Arguments("{ \"allOf\": [ { \"$ref\": \"#/definitions/Missing\" } ] }", "the $ref \"#/definitions/Missing\" doesn't point at one of its definitions")]
    public async Task InlineSchemaThatIsNotAFragment_FailsWithOPT1005(string json, string problem)
    {
        var output = await BuildWithItem("HttpClients:Petstore", new() { ["Schema"] = json });

        await output.Should().Contain(
            $"error OPT1005: The OptionarySchemaFragment item \"HttpClients:Petstore\": its Schema metadata isn't a configuration schema fragment: {problem}"
        );
    }

    [Test]
    public async Task FileThatIsNotAFragment_FailsWithOPT1005()
    {
        var output = await BuildWithItem("HttpClients:Petstore", new() { ["File"] = "fragment.schema.json" }, "[1, 2]");

        await output.Should().Contain("error OPT1005: The OptionarySchemaFragment item \"HttpClients:Petstore\": its file \"");
        await output.Should().Contain("lib/fragment.schema.json\" isn't a configuration schema fragment: the schema is not a JSON object.");
    }

    [Test]
    public async Task EditingTheFile_UpdatesTheSchema()
    {
        using var project = new OptionaryTestProject();
        project
           .AddProject(
                "host/host.csproj",
                ProjectCreator
                   .Templates.SdkCsproj(targetFramework: TargetFramework, outputType: "Exe")
                   .ItemInclude("OptionarySchemaFragment", "HttpClients:Petstore", metadata: SchemaFragmentItemTests.PetstoreFile)
            )
           .AddFile("host/Program.cs", Fragments.Program)
           .AddFile("host/petstore.schema.json", SchemaFragmentItemTests.Petstore);
        const string schema = "host/obj/appsettings.schema.json";

        await project.BuildProject("host");
        await SchemaFragmentItemTests.PetstoreDescription(project.ReadFile(schema)).Should().BeEqualTo(SchemaFragmentItemTests.PetstoreDescription(SchemaFragmentItemTests.Petstore));

        var edited = SchemaFragmentItemTests.Petstore.Replace("Petstore client;", "Petstore client; edited;", StringComparison.Ordinal);
        project.AddFile("host/petstore.schema.json", edited);
        await project.BuildProject("host");

        await SchemaFragmentItemTests.PetstoreDescription(project.ReadFile(schema)).Should().StartWith("Petstore client; edited;");
    }
}

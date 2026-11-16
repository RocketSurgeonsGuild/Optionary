using Microsoft.Build.Utilities.ProjectCreation;

namespace Optionary.Build.Tests;

/// <summary>
///     Covers <c>Optionary.Schema.EditorSettings.targets</c>: with <c>OptionaryScaffoldEditorSettings=true</c>, building a host maps
///     its merged schema in <c>.vscode/settings.json</c>, touching nothing else in the file; without it nothing is written. The
///     editor itself is covered by the Optionary.Build.Tasks tests.
/// </summary>
public class ConfigurationSchemaEditorSettingsTests
{
    private const string TargetFramework = "net10.0";
    private const string HostSchemaUrl = "\"./host/obj/appsettings.schema.json\"";

    private static OptionaryTestProject CreateHost(bool repository)
    {
        var project = new OptionaryTestProject();
        project
           .AddProject("host/host.csproj", ProjectCreator.Templates.SdkCsproj(targetFramework: TargetFramework, outputType: "Exe"))
           .AddFile("host/Program.cs", Fragments.Program)
           .AddFile("host/Fragments.cs", Fragments.App);
        if (repository) project.AddFile(".git/HEAD", "ref: refs/heads/main\n");
        return project;
    }

    private static string RepositorySettings(OptionaryTestProject project) => project.FullPath(".vscode/settings.json");

    [Test]
    public async Task PropertyOff_WritesNothing()
    {
        using var project = CreateHost(repository: true);

        await project.BuildProject("host");

        await project.Exists("host/obj/appsettings.schema.json").Should().BeTrue();
        await Directory.Exists(project.FullPath(".vscode")).Should().BeFalse();
        await Directory.Exists(project.FullPath("host/.vscode")).Should().BeFalse();
    }

    [Test]
    public async Task PropertyOn_CreatesTheSettingsAtTheRepositoryRoot_AndLeavesAnUpToDateFileAlone()
    {
        using var project = CreateHost(repository: true);

        await project.BuildProject("host", "-p:OptionaryScaffoldEditorSettings=true");

        var settings = RepositorySettings(project);
        var text = File.ReadAllText(settings);
        await text.Should().Contain($"\"url\": {HostSchemaUrl}");
        await text.Should().Contain($"\"yaml.schemas\": {{\n    {HostSchemaUrl}: [\"host/appsettings*.yaml\", \"host/appsettings*.yml\"]");
        await text.Should().Contain("\"evenBetterToml.schema.associations\"");
        await text.Should().Contain("\"/host/appsettings*.json\"");
        await project.Warnings("host").Should().BeEmpty();
        await Directory.Exists(project.FullPath("host/.vscode")).Should().BeFalse();

        var written = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(settings, written);

        await project.BuildProject("host", "-p:OptionaryScaffoldEditorSettings=true");

        await File.GetLastWriteTimeUtc(settings).Should().BeEqualTo(written);
        await File.ReadAllText(settings).Should().BeEqualTo(text);
    }

    [Test]
    public async Task PropertyOn_UpdatesAnExistingFile_KeepingEverythingElse()
    {
        using var project = CreateHost(repository: true);
        project.AddFile(".vscode/settings.json", "{\r\n\t// my editor\r\n\t\"editor.fontSize\": 14, /* big */\r\n}\r\n");

        await project.BuildProject("host", "-p:OptionaryScaffoldEditorSettings=true");

        var text = File.ReadAllText(RepositorySettings(project));
        await text.Should().StartWith("{\r\n\t// my editor\r\n\t\"editor.fontSize\": 14, /* big */\r\n\t\"json.schemas\": [");
        await text.Should().Contain($"\"url\": {HostSchemaUrl}");
        await text.Replace("\r\n", "", StringComparison.Ordinal).Should().NotContain("\n");
    }

    [Test]
    public async Task PropertyOn_WithoutARepository_UsesTheProjectDirectory_AndTheDirectoryPropertyOverrides()
    {
        using var project = CreateHost(repository: false);

        await project.BuildProject("host", "-p:OptionaryScaffoldEditorSettings=true -p:OptionaryEditorSettingsDirectory=..");

        await File.ReadAllText(RepositorySettings(project)).Should().Contain($"\"url\": {HostSchemaUrl}");

        // No .git entry anywhere above the scaffold (it is in the temp folder), so the project directory is the default.
        File.Delete(RepositorySettings(project));
        await project.BuildProject("host", "-p:OptionaryScaffoldEditorSettings=true");

        await project.ReadFile("host/.vscode/settings.json").Should().Contain("\"url\": \"./obj/appsettings.schema.json\"");
    }

    [Test]
    public async Task PropertyOn_ForALibrary_OrWithTheSchemaTurnedOff_WritesNothing()
    {
        using var project = CreateHost(repository: true);
        project
           .AddProject("lib/lib.csproj", ProjectCreator.Templates.SdkCsproj(targetFramework: TargetFramework))
           .AddFile("lib/Fragments.cs", Fragments.Greetings);

        await project.BuildProject("lib", "-p:OptionaryScaffoldEditorSettings=true");
        await project.BuildProject("host", "-p:OptionaryScaffoldEditorSettings=true -p:OptionarySchema=false");

        await Directory.Exists(project.FullPath(".vscode")).Should().BeFalse();
    }

    [Test]
    public async Task PropertyOn_WithAFileItCantUpdate_WarnsAndLeavesItAlone()
    {
        using var project = CreateHost(repository: true);
        const string unparsable = "{ \"a\": 1 \"b\": 2 }";
        project.AddFile(".vscode/settings.json", unparsable);

        await project.BuildProject("host", "-p:OptionaryScaffoldEditorSettings=true");

        await File.ReadAllText(RepositorySettings(project)).Should().BeEqualTo(unparsable);
        var warning = await project.Warnings("host")
                                   .Where(z => z.Contains("left", StringComparison.Ordinal) && z.Contains("settings.json", StringComparison.Ordinal))
                                   .ToArray()
                                   .Should()
                                   .HaveSingleItem();
        await warning.Should().Contain("\"json.schemas\": [");
        await warning.Should().Contain(HostSchemaUrl);
    }

    [Test]
    public async Task PropertyOn_WithAReadOnlyFile_WarnsAndLeavesItAlone()
    {
        using var project = CreateHost(repository: true);
        project.AddFile(".vscode/settings.json", "{ \"a\": 1 }");
        var settings = RepositorySettings(project);
        File.SetAttributes(settings, FileAttributes.ReadOnly);
        try
        {
            await project.BuildProject("host", "-p:OptionaryScaffoldEditorSettings=true");

            await File.ReadAllText(settings).Should().BeEqualTo("{ \"a\": 1 }");
            await project.Warnings("host").Where(z => z.Contains("read-only", StringComparison.Ordinal)).ToArray().Should().HaveSingleItem();
        }
        finally
        {
            File.SetAttributes(settings, FileAttributes.Normal);
        }
    }
}

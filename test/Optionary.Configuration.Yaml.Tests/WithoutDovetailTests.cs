namespace Optionary.Configuration.Yaml.Tests;

/// <summary>
///     The package's build targets declare the three YAML joints as <c>DovetailTemplate</c> items in applications and test projects only
///     (decision D12). Only Dovetail's <c>Joinery.Dovetail.Templates.targets</c> reads them, so a project without Dovetail compiles
///     nothing from them. Writing them into a Dovetail application is Dovetail's to test (RocketSurgeonsGuild/Dovetail#421).
/// </summary>
[ClassDataSource<ConsumerProjects>(Shared = SharedType.PerTestSession)]
public sealed class WithoutDovetailTests(ConsumerProjects consumers)
{
    private static readonly string[] Joints = ["YamlBrowserJoint", "YamlEmbeddedJoint", "YamlJoint"];

    [Test]
    [Arguments("app")]
    [Arguments("tests")]
    public async Task ApplicationsAndTestProjects_DeclareTheThreeJointTemplates(string project)
    {
        var result = await consumers.MSBuild(project, [], "DovetailTemplate");
        await result.ExitCode.Should().BeEqualTo(0).Because(result.Output);

        var templates = result.Items["DovetailTemplate"];

        await templates.Select(z => z.Identity).Order(StringComparer.Ordinal).Should().BeEquivalentTo(Joints, CollectionOrdering.Matching);
        foreach (var template in templates)
        {
            await template["Package"].Should().BeEqualTo("Optionary.Configuration.Yaml");
            await template["OutputPrefix"].Should().BeEqualTo("OptionaryYaml");
            await Path.GetFileName(template["File"]).Should().BeEqualTo($"{template.Identity}.cs.template");
            await File.Exists(template["File"]).Should().BeTrue().Because(template["File"]);
        }
    }

    [Test]
    public async Task Libraries_DeclareNoJointTemplates()
    {
        var result = await consumers.MSBuild("lib", [], "DovetailTemplate");
        await result.ExitCode.Should().BeEqualTo(0).Because(result.Output);

        await result.Items["DovetailTemplate"].Should().BeEmpty();
    }

    [Test]
    public async Task AnApplicationWithoutDovetail_CompilesNothingFromTheTemplates()
    {
        var result = await consumers.MSBuild("app", ["Build"], "Compile", "DovetailTemplate");
        await result.ExitCode.Should().BeEqualTo(0).Because(result.Output);

        // The templates are declared (the gate is open)...
        await result.Items["DovetailTemplate"].Count.Should().BeEqualTo(3);
        // ...but nothing reads them: the application compiles only its own source and the SDK's generated files.
        var compiled = result.Items["Compile"].Select(z => Path.GetFileName(z.Identity)).ToArray();
        await compiled.Should().Contain("Program.cs");
        await compiled.Where(z => z.Contains("Yaml", StringComparison.OrdinalIgnoreCase)).Should().BeEmpty();
        var generated = Directory.EnumerateFiles(Path.Combine(consumers.Directory, "app", "obj"), "*.cs", SearchOption.AllDirectories)
                                 .Select(Path.GetFileName)
                                 .Where(z => z!.Contains("Yaml", StringComparison.OrdinalIgnoreCase) || z.StartsWith("OptionaryYaml.", StringComparison.Ordinal))
                                 .ToArray();
        await generated.Should().BeEmpty();
    }
}

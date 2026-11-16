using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Optionary.Configuration.Yaml.Tests;

/// <summary>
///     <see cref="YamlConfigurationExtensions.AddYamlFile(IConfigurationBuilder, string, bool, bool)" /> and
///     <see cref="YamlConfigurationProvider" />: loading a file from the base path, optional files, and reload on change.
/// </summary>
public sealed class YamlFileTests : IDisposable
{
    private readonly string _directory = Directory.CreateDirectory(
        Path.Combine(Path.GetTempPath(), "optionary-yaml-tests-" + Guid.NewGuid().ToString("N"))
    ).FullName;

    [Test]
    public async Task AFile_IsReadFromTheBasePath()
    {
        await File.WriteAllTextAsync(Path.Combine(_directory, "appsettings.yaml"), "Section:\n  Value: from file\n");

        var configuration = new ConfigurationBuilder().SetBasePath(_directory).AddYamlFile("appsettings.yaml").Build();

        await configuration["Section:Value"].Should().BeEqualTo("from file");
        await configuration.Providers.Single().Should().BeOfType(typeof(YamlConfigurationProvider));
    }

    [Test]
    public async Task AMissingOptionalFile_AddsNothing()
    {
        var configuration = new ConfigurationBuilder().SetBasePath(_directory).AddYamlFile("missing.yaml", optional: true).Build();

        await configuration.AsEnumerable().Should().BeEmpty();
    }

    [Test]
    public async Task AMissingRequiredFile_Throws()
    {
        var build = () => new ConfigurationBuilder().SetBasePath(_directory).AddYamlFile("missing.yaml").Build();

        await build.Should().Throw<FileNotFoundException>();
    }

    [Test]
    public async Task MalformedYamlInAFile_IsAnInvalidDataException()
    {
        await File.WriteAllTextAsync(Path.Combine(_directory, "broken.yaml"), "Name: [unterminated");

        var build = () => new ConfigurationBuilder().SetBasePath(_directory).AddYamlFile("broken.yaml").Build();

        // FileConfigurationProvider wraps the provider's FormatException.
        await build.Should().Throw<InvalidDataException>();
    }

    [Test]
    public async Task AChangedFile_IsReloaded_WhenReloadOnChangeIsSet()
    {
        var path = Path.Combine(_directory, "appsettings.yaml");
        await File.WriteAllTextAsync(path, "Greeting: initial\n");
        var configuration = new ConfigurationBuilder()
                           .SetBasePath(_directory)
                           .AddYamlFile("appsettings.yaml", optional: false, reloadOnChange: true)
                           .Build();
        await configuration["Greeting"].Should().BeEqualTo("initial");

        var reloaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = ChangeToken.OnChange(
            configuration.GetReloadToken,
            () =>
            {
                if (configuration["Greeting"] == "updated") reloaded.TrySetResult();
            }
        );

        // Give the file watcher a moment to attach before changing the file.
        await Task.Delay(TimeSpan.FromMilliseconds(250));
        await File.WriteAllTextAsync(path, "Greeting: updated\n");

        await reloaded.Task.WaitAsync(TimeSpan.FromSeconds(15));
        await configuration["Greeting"].Should().BeEqualTo("updated");
    }

    [Test]
    public async Task AConfiguredSource_IsBuiltIntoAYamlProvider()
    {
        await File.WriteAllTextAsync(Path.Combine(_directory, "custom.yml"), "Name: configured\n");

        var configuration = new ConfigurationBuilder()
                           .SetBasePath(_directory)
                           .AddYamlFile(source => source.Path = "custom.yml")
                           .Build();

        await configuration["Name"].Should().BeEqualTo("configured");
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, true);
        }
        catch (IOException) { }
    }
}

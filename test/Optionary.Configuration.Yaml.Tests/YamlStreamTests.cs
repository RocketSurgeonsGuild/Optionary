using System.Text;
using Microsoft.Extensions.Configuration;

namespace Optionary.Configuration.Yaml.Tests;

/// <summary>
///     How <see cref="YamlConfigurationExtensions.AddYamlStream" /> flattens a YAML document into configuration keys.
/// </summary>
public sealed class YamlStreamTests
{
    [Test]
    public async Task Scalars_BecomeTopLevelKeys()
    {
        var configuration = Load(
            """
            Name: optionary
            Port: 8080
            Enabled: true
            """
        );

        await configuration["Name"].Should().BeEqualTo("optionary");
        await configuration["Port"].Should().BeEqualTo("8080");
        await configuration["Enabled"].Should().BeEqualTo("true");
    }

    [Test]
    public async Task NestedMaps_JoinTheirKeysWithColons()
    {
        var configuration = Load(
            """
            Logging:
              LogLevel:
                Default: Warning
                Optionary: Debug
            """
        );

        await configuration["Logging:LogLevel:Default"].Should().BeEqualTo("Warning");
        await configuration["Logging:LogLevel:Optionary"].Should().BeEqualTo("Debug");
    }

    [Test]
    public async Task Keys_AreCaseInsensitive()
    {
        var configuration = Load("Logging: { Level: Debug }");

        await configuration["logging:level"].Should().BeEqualTo("Debug");
    }

    [Test]
    public async Task Sequences_UseTheirIndexesAsKeys()
    {
        var configuration = Load(
            """
            Hosts:
              - alpha
              - beta
            Servers:
              - Name: primary
                Port: 80
              - Name: secondary
                Port: 81
            """
        );

        await configuration["Hosts:0"].Should().BeEqualTo("alpha");
        await configuration["Hosts:1"].Should().BeEqualTo("beta");
        await configuration["Servers:0:Name"].Should().BeEqualTo("primary");
        await configuration["Servers:0:Port"].Should().BeEqualTo("80");
        await configuration["Servers:1:Name"].Should().BeEqualTo("secondary");
        await configuration["Servers:1:Port"].Should().BeEqualTo("81");
        await configuration.GetSection("Hosts").GetChildren().Select(z => z.Key).Should().BeEquivalentTo(["0", "1"]);
    }

    [Test]
    public async Task PlainNullScalars_AreNull()
    {
        var configuration = Load(
            """
            Tilde: ~
            Lower: null
            Title: Null
            Upper: NULL
            """
        );

        var data = Data(configuration);
        await data.Should().ContainKey("Tilde");
        await data["Tilde"].Should().BeNull();
        await data["Lower"].Should().BeNull();
        await data["Title"].Should().BeNull();
        await data["Upper"].Should().BeNull();
    }

    [Test]
    public async Task QuotedNull_IsTheString()
    {
        var configuration = Load(
            """
            Double: "null"
            Single: '~'
            """
        );

        await configuration["Double"].Should().BeEqualTo("null");
        await configuration["Single"].Should().BeEqualTo("~");
    }

    [Test]
    public async Task EmptyValues_AreEmptyStrings()
    {
        var configuration = Load(
            """
            Missing:
            Quoted: ""
            """
        );

        await configuration["Missing"].Should().BeEqualTo("");
        await configuration["Quoted"].Should().BeEqualTo("");
    }

    [Test]
    public async Task Comments_AreIgnored()
    {
        var configuration = Load(
            """
            # a full-line comment
            Name: optionary # a trailing comment
            Section:
              # inside a map
              Value: "# not a comment"
            """
        );

        await Data(configuration).Keys.Should().BeEquivalentTo(["Name", "Section:Value"]);
        await configuration["Name"].Should().BeEqualTo("optionary");
        await configuration["Section:Value"].Should().BeEqualTo("# not a comment");
    }

    [Test]
    public async Task AnEmptyDocument_HasNoKeys()
    {
        var configuration = Load("");

        await Data(configuration).Should().BeEmpty();
    }

    [Test]
    public async Task KeysThatDifferOnlyInCase_AreDuplicates()
    {
        var load = () => Load(
            """
            Name: one
            name: two
            """
        );

        await load.Should().Throw<FormatException>();
    }

    [Test]
    public async Task MalformedYaml_IsAFormatException()
    {
        var load = () => Load("Name: [unterminated");

        await load.Should().Throw<FormatException>();
    }

    [Test]
    public async Task EachStream_IsItsOwnSource_AndLaterStreamsWin()
    {
        var configuration = new ConfigurationBuilder()
                           .AddYamlStream(Stream("Name: first\nOnlyFirst: yes"))
                           .AddYamlStream(Stream("Name: second"))
                           .Build();

        await configuration.Providers.Count().Should().BeEqualTo(2);
        await configuration["Name"].Should().BeEqualTo("second");
        await configuration["OnlyFirst"].Should().BeEqualTo("yes");
    }

    /// <summary>
    ///     The keys the single provider holds, including those with a <see langword="null" /> value (which
    ///     <see cref="IConfiguration" /> can't tell apart from a missing key).
    /// </summary>
    private static Dictionary<string, string?> Data(IConfigurationRoot configuration)
    {
        var provider = configuration.Providers.Single();
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, _) in configuration.AsEnumerable())
        {
            if (provider.TryGet(key, out var value)) data[key] = value;
        }

        return data;
    }

    private static MemoryStream Stream(string yaml) => new(Encoding.UTF8.GetBytes(yaml));

    private static IConfigurationRoot Load(string yaml) => new ConfigurationBuilder().AddYamlStream(Stream(yaml)).Build();
}

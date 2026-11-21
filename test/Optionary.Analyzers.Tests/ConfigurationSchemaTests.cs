using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Rocket.Surgery.Extensions.Testing.SourceGenerators;

namespace Optionary.Analyzers.Tests;

/// <summary>
///     The JSON Schema fragments <c>[Optionary]</c> produces. Each snapshot shows every fragment the compilation
///     carries (read back from its <c>AssemblyMetadata</c>, pretty-printed) followed by the generator's diagnostics.
/// </summary>
public class ConfigurationSchemaTests() : GeneratorTest()
{
    private const string Usings = """
        using System;
        using System.Collections.Generic;
        using System.ComponentModel;
        using System.ComponentModel.DataAnnotations;
        using Optionary;
        using Microsoft.Extensions.Configuration;

        """;

    private GeneratorTestContextBuilder WithAnnotations() =>
        ConfigurationBuilder.AddConfigurationOptionsReferences().AddReferences(typeof(RangeAttribute), typeof(DescriptionAttribute), typeof(TypeConverterAttribute), typeof(Uri));

    [Test]
    public async Task Describes_an_options_class()
    {
        var result = await WithAnnotations()
                          .AddSources(
                               Usings
                             + """
                               namespace Sample;

                               public enum GreetingMode { Polite, Casual, Shouty }

                               [Flags]
                               public enum Channels { None = 0, Console = 1, Email = 2 }

                               /// <summary>Settings for the greeting service.</summary>
                               [Optionary]
                               public class GreetingsOptions
                               {
                                   /// <summary>Text placed before the name.</summary>
                                   public string Message { get; set; } = "Hello";

                                   /// <summary>How many times to greet.</summary>
                                   [Range(1, 10)]
                                   public int Count { get; set; } = 1;

                                   [Description("Turns greetings off.")]
                                   public bool Enabled { get; set; } = true;

                                   public GreetingMode Mode { get; set; } = GreetingMode.Polite;

                                   public Channels Channels { get; set; }

                                   public TimeSpan Timeout { get; set; }

                                   public Uri? Endpoint { get; set; }

                                   [EmailAddress]
                                   public string? ReplyTo { get; set; }

                                   [RegularExpression("[a-z]{2}-[A-Z]{2}")]
                                   public string Culture { get; set; } = "en-US";

                                   [Obsolete("Use Message.")]
                                   public string? OldGreeting { get; set; }

                                   [ConfigurationKeyName("custom_key")]
                                   public string? Custom { get; set; }

                                   public byte Level { get; set; }

                                   public double? Ratio { get; set; }

                                   /// <summary>Who to greet.</summary>
                                   [MaxLength(5)]
                                   public List<string> Names { get; } = [];

                                   public Dictionary<string, int> Weights { get; set; } = [];

                                   public Dictionary<GreetingMode, string> Templates { get; set; } = [];

                                   public RetryOptions Retry { get; set; } = new();

                                   public int Total => Count * 2;
                               }

                               /// <summary>How to retry.</summary>
                               public class RetryOptions
                               {
                                   /// <summary>Attempts before giving up.</summary>
                                   [Range(0, 5)]
                                   public int Attempts { get; set; } = 3;

                                   public TimeSpan[] Delays { get; set; } = [];
                               }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(Describe(result));
    }

    [Test]
    public async Task Names_sections()
    {
        var result = await WithAnnotations()
                          .AddSources(
                               Usings
                             + """
                               namespace Sample;

                               [Optionary]
                               public class HostingSettings { public string? Name { get; set; } }

                               [Optionary("Http:Retry")]
                               public class RetryPolicy { public int Count { get; set; } }

                               [Optionary("Shared:A")]
                               public class SharedAOptions { public string? Value { get; set; } }

                               [Optionary("Shared:B")]
                               public class SharedBOptions { public string? Value { get; set; } }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(Describe(result));
    }

    [Test]
    public async Task Describes_constructor_bound_types()
    {
        var result = await WithAnnotations()
                          .AddSources(
                               Usings
                             + """
                               namespace Sample;

                               /// <summary>A server to connect to.</summary>
                               /// <param name="Host">The host name.</param>
                               /// <param name="Port">The port to connect on.</param>
                               [Optionary]
                               public record ServerOptions(string Host, int Port = 8080)
                               {
                                   /// <summary>Whether to use TLS.</summary>
                                   public bool Secure { get; init; }
                               }

                               [Optionary]
                               public class ConnectionOptions(string server)
                               {
                                   /// <summary>The server's address.</summary>
                                   public string Server { get; } = server;

                                   public int Timeout { get; set; } = 30;
                               }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(Describe(result));
    }

    [Test]
    public async Task Reports_problems()
    {
        var result = await WithAnnotations()
                          .WithDiagnosticSeverity(DiagnosticSeverity.Warning)
                          .AddSources(
                               Usings
                             + """
                               namespace Sample;

                               [Optionary]
                               public abstract class AbstractOptions { }

                               [Optionary]
                               public class GenericOptions<T> { }

                               [Optionary]
                               public class Options { }

                               [Optionary("A::B")]
                               public class BadPathOptions { }

                               [Optionary]
                               public class UnsupportedOptions
                               {
                                   public object? Anything { get; set; }
                                   public System.Globalization.CultureInfo? Culture { get; set; }
                                   public Action? Callback { get; set; }
                                   public Dictionary<int, string> ByNumber { get; set; } = [];
                                   public string? Kept { get; set; }
                               }

                               [Optionary]
                               public class CollidingOptions
                               {
                                   public string? Name { get; set; }

                                   [ConfigurationKeyName("name")]
                                   public string? Other { get; set; }
                               }

                               [Optionary]
                               public class AmbiguousOptions
                               {
                                   public AmbiguousOptions(int a) { }
                                   public AmbiguousOptions(string b) { }
                                   public int A { get; set; }
                                   public string? B { get; set; }
                               }

                               [Optionary]
                               public class OrphanParameterOptions(string unmatched)
                               {
                                   public string? Name { get; set; }
                               }

                               [Optionary]
                               public class TreeOptions
                               {
                                   public string? Name { get; set; }
                                   public TreeOptions? Child { get; set; }
                               }

                               [Optionary]
                               public class AnnotationOptions
                               {
                                   [Range(1, 10)]
                                   public string? Text { get; set; }

                                   [RegularExpression("(?i)abc")]
                                   public string? Pattern { get; set; }

                                   [RegularExpression("[a-z-[aeiou]]+")]
                                   public string? Consonants { get; set; }

                                   [MaxLength(3)]
                                   public int Number { get; set; }
                               }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(Describe(result));
    }

    [Test]
    public async Task Describes_types_from_referenced_assemblies_with_their_xml_docs()
    {
        var thirdParty = await ConfigurationBuilder
                              .WithProjectName("ThirdParty")
                              .AddSources(
                                   """
                                   namespace ThirdParty;

                                   public class ClientOptions
                                   {
                                       public string? BaseAddress { get; set; }
                                       public int Retries { get; set; }
                                       public ClientLimits Limits { get; set; } = new();
                                   }

                                   public class ClientLimits
                                   {
                                       public int MaxConnections { get; set; }
                                   }
                                   """
                               )
                              .Build()
                              .GenerateAsync(TestContext.CancellationToken);

        const string documentationPath = "/packages/thirdparty/1.0.0/lib/net10.0/ThirdParty.xml";
        var result = await ConfigurationBuilder
                          .AddConfigurationOptionsReferences()
                          .AddCompilationReferences(thirdParty)
                          .AddSources("[assembly: Optionary.Optionary<ThirdParty.ClientOptions>(\"Client\")]")
                          .AddAdditionalText(
                               documentationPath,
                               """
                               <?xml version="1.0"?>
                               <doc>
                                 <assembly><name>ThirdParty</name></assembly>
                                 <members>
                                   <member name="T:ThirdParty.ClientOptions"><summary>Options for the <see cref="T:ThirdParty.ClientOptions"/> client.</summary></member>
                                   <member name="P:ThirdParty.ClientOptions.BaseAddress"><summary>Where requests go.</summary></member>
                                   <member name="P:ThirdParty.ClientOptions.Retries"><summary>Retries per request.</summary></member>
                                   <member name="P:ThirdParty.ClientLimits.MaxConnections"><summary>Connections per server.</summary></member>
                                 </members>
                               </doc>
                               """
                           )
                          .AddOption(documentationPath, "build_metadata.AdditionalFiles.OptionaryReferenceDocumentation", "true")
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(Describe(result));
    }

    [Test]
    public async Task Lists_required_members_when_asked()
    {
        var result = await WithAnnotations()
                          .AddGlobalOption("build_property.OptionarySchemaRequired", "true")
                          .AddSources(
                               Usings
                             + """
                               namespace Sample;

                               [Optionary]
                               public record DatabaseOptions(string ConnectionString, int Port, int? Timeout = null)
                               {
                                   [Required]
                                   public string? Schema { get; init; }

                                   public required string Provider { get; init; }

                                   public bool Pooling { get; init; }
                               }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(Describe(result));
    }

    [Test]
    public async Task Describes_framework_and_collection_types()
    {
        var result = await WithAnnotations()
                          .AddReferences(typeof(System.Net.IPAddress), typeof(SortedDictionary<string, int>), typeof(Queue<int>), typeof(TypeConverter))
                          .AddSources(
                               Usings
                             + """
                               namespace Sample;

                               public class Tags : List<string> { }

                               [TypeConverter(typeof(TypeConverter))]
                               public class Money { public decimal Amount { get; set; } public string? Currency { get; set; } }

                               [Optionary]
                               public class TypesOptions
                               {
                                   public System.Globalization.CultureInfo? Culture { get; set; }
                                   public Half Ratio { get; set; }
                                   public Int128 Big { get; set; }
                                   public UInt128 BigUnsigned { get; set; }
                                   public object? Anything { get; set; }
                                   public IConfigurationSection? Raw { get; set; }
                                   public Tags Labels { get; set; } = new();
                                   public System.Collections.Generic.SortedDictionary<string, int> Sorted { get; set; } = new();
                                   public Dictionary<int, string> ByNumber { get; set; } = [];
                                   public Dictionary<uint, string> ByUnsigned { get; set; } = [];
                                   public Money Price { get; set; } = new();
                                   public DateTime At { get; set; }
                                   public DateOnly Day { get; set; }
                                   public TimeOnly Time { get; set; }
                                   public System.Net.IPAddress? Address { get; set; }
                                   public Queue<int> Pending { get; set; } = new();
                               }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(Describe(result));
    }

    [Test]
    public async Task Describes_nested_objects_without_requiring_their_constructors()
    {
        var result = await WithAnnotations()
                          .AddSources(
                               Usings
                             + """
                               namespace Sample;

                               public class Limits
                               {
                                   internal Limits() { }
                                   public int MaxConnections { get; set; }
                               }

                               public class Seeded(int seed)
                               {
                                   public int Value { get; set; } = seed;
                               }

                               [Optionary]
                               public class ClientOptions
                               {
                                   public Limits Limits { get; } = new();
                                   public Seeded Seeded { get; set; } = new(1);
                               }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(Describe(result));
    }

    [Test]
    public async Task Reports_invalid_declarations()
    {
        var result = await WithAnnotations()
                          .WithDiagnosticSeverity(DiagnosticSeverity.Warning)
                          .AddSources(
                               Usings
                             + """
                               namespace Sample;

                               [Optionary]
                               public class Tags : List<string> { }

                               public class Outer<T>
                               {
                                   [Optionary]
                                   public class InnerOptions { public string? Value { get; set; } }
                               }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(Describe(result));
    }

    [Test]
    public async Task Skips_empty_string_defaults()
    {
        var result = await WithAnnotations()
                          .AddSources(
                               Usings
                             + """
                               namespace Sample;

                               [Optionary]
                               public class BlankOptions
                               {
                                   /// <summary>Who to greet.</summary>
                                   public string Name { get; set; } = "";

                                   public string Greeting { get; set; } = "Hi";
                               }

                               [Optionary]
                               public record BlankRecordOptions(string Name = "", string Greeting = "Hi");
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await result.FinalDiagnostics.Where(z => z.Severity == DiagnosticSeverity.Error).Should().BeEmpty();
        foreach (var section in new[] { "Blank", "BlankRecord" })
        {
            using var document = JsonDocument.Parse(Fragments(result)[section]);
            var properties = document.RootElement.GetProperty("properties");
            var name = properties.GetProperty("Name");
            await name.TryGetProperty("default", out _).Should().BeFalse().Because(section);
            await ( name.TryGetProperty("description", out var description) ? description.GetString() ?? "" : "" ).Should().NotContain("Default:").Because(section);
            await properties.GetProperty("Greeting").GetProperty("default").GetString().Should().BeEqualTo("Hi").Because(section);
            await properties.GetProperty("Greeting").GetProperty("description").GetString()!.Should().Contain("Default: Hi.").Because(section);
        }
    }

    [Test]
    public async Task Rejects_a_type_configured_by_both_the_class_and_the_assembly_form()
    {
        var result = await WithAnnotations()
                          .AddSources(
                               Usings
                             + """
                               [assembly: Optionary<Sample.TwiceOptions>("Second")]
                               [assembly: Optionary<Sample.AssemblyOnlyOptions>("Elsewhere")]

                               namespace Sample;

                               [Optionary("First")]
                               public class TwiceOptions { public string? Value { get; set; } }

                               public class AssemblyOnlyOptions { public string? Value { get; set; } }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        // Two assembly attributes for different types must compile (the attribute has to allow several uses on the assembly).
        await result.FinalDiagnostics.Where(z => z.Severity == DiagnosticSeverity.Error && z.Id.StartsWith("CS", StringComparison.Ordinal)).Should().BeEmpty();
        var diagnostics = result.Results[typeof(ConfigurationGenerator)].Diagnostics;
        await diagnostics.Select(z => z.Id).Should().BeEquivalentTo(["OPT0010"], CollectionOrdering.Matching);
        await diagnostics[0].GetMessage(CultureInfo.InvariantCulture).Should().BeEqualTo("'Sample.TwiceOptions' is configured more than once; a type can have only one [Optionary]");
        // The assembly attribute is the one reported, and only the class declaration produces a fragment.
        await diagnostics[0].Location.SourceTree!.ToString().Substring(diagnostics[0].Location.SourceSpan.Start, diagnostics[0].Location.SourceSpan.Length).Should().Contain("\"Second\"");
        await Fragments(result).Keys.Should().BeEquivalentTo(["First", "Elsewhere"]);
    }

    [Test]
    public async Task Rejects_a_type_named_twice_in_the_assembly_form()
    {
        var result = await WithAnnotations()
                          .AddSources(
                               Usings
                             + """
                               [assembly: Optionary<Sample.RepeatedOptions>("First")]
                               [assembly: Optionary<Sample.RepeatedOptions>("Second")]

                               namespace Sample;

                               public class RepeatedOptions { public string? Value { get; set; } }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await result.FinalDiagnostics.Where(z => z.Severity == DiagnosticSeverity.Error && z.Id.StartsWith("CS", StringComparison.Ordinal)).Should().BeEmpty();
        var diagnostics = result.Results[typeof(ConfigurationGenerator)].Diagnostics;
        await diagnostics.Select(z => z.Id).Should().BeEquivalentTo(["OPT0010"], CollectionOrdering.Matching);
        // The first declaration wins and the repeat produces nothing.
        await Fragments(result).Keys.Should().BeEquivalentTo(["First"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Inherits_key_names_and_accepts_escaped_patterns()
    {
        var result = await WithAnnotations()
                          .AddSources(
                               Usings
                             + """
                               namespace Sample;

                               public class BaseOptions
                               {
                                   [ConfigurationKeyName("base_key")]
                                   public virtual string? Name { get; set; }
                               }

                               [Optionary]
                               public class DerivedOptions : BaseOptions
                               {
                                   public override string? Name { get; set; }

                                   [RegularExpression(@"C:\\Apps\\.*")]
                                   public string? Folder { get; set; }

                                   [RegularExpression(@"\(?id\)?")]
                                   public string? Parenthesized { get; set; }
                               }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(Describe(result));
    }

    [Test]
    public async Task Describes_the_declared_members_of_interfaces_and_abstract_classes()
    {
        // The binder can't create these, but fills an existing instance's declared properties, so the keys are valid.
        var result = await WithAnnotations()
                          .AddSources(
                               Usings
                             + """
                               namespace Sample;

                               public interface IShape { int Sides { get; set; } }
                               public class Square : IShape { public int Sides { get; set; } = 4; public string? Fill { get; set; } }
                               public abstract class Base { public string? Name { get; set; } }
                               public class Derived : Base { public string? Extra { get; set; } }

                               [Optionary]
                               public class ShapesOptions
                               {
                                   public IShape Shape { get; set; } = new Square();
                                   public Base Item { get; } = new Derived();
                               }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await result.FinalDiagnostics.Where(z => z.Severity == DiagnosticSeverity.Error).Should().BeEmpty();
        await result.Results[typeof(ConfigurationGenerator)].Diagnostics.Should().BeEmpty();
        using var shapes = JsonDocument.Parse(Fragments(result)["Shapes"]);
        var definitions = shapes.RootElement.GetProperty("definitions");
        await definitions.GetProperty("Sample.IShape").GetProperty("properties").EnumerateObject().Select(z => z.Name).Should().BeEquivalentTo(["Sides"], CollectionOrdering.Matching);
        await definitions.GetProperty("Sample.Base").GetProperty("properties").EnumerateObject().Select(z => z.Name).Should().BeEquivalentTo(["Name"], CollectionOrdering.Matching);
        await shapes.RootElement.GetProperty("properties").EnumerateObject().Select(z => z.Name).Should().BeEquivalentTo(["Shape", "Item"]);
    }

    [Test]
    public async Task Survives_empty_comments()
    {
        var result = await WithAnnotations()
                          .AddSources(
                               Usings
                             + """
                               namespace Sample;

                               /**/
                               [Optionary]
                               public class QuietOptions
                               {
                                   /**/
                                   public string? Name { get; set; }
                               }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        var text = Describe(result);
        await text.Should().Contain("## Quiet");
        await text.Should().NotContain("OPT0009");
    }

    [Test]
    public async Task Locates_diagnostics_in_source_so_pragmas_apply()
    {
        var result = await WithAnnotations()
                          .AddSources(
                               Usings
                             + """
                               namespace Sample;

                               [Optionary]
                               public class CallbackOptions { public Action? Callback { get; set; } }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        var diagnostics = result.Results[typeof(ConfigurationGenerator)].Diagnostics;
        await diagnostics.Should().Contain(z => z.Id == "OPT0004");
        await diagnostics.Should().All(z => z.Location.SourceTree != null);
    }

    /// <summary>
    ///     Each object type is described once and referenced, so a deep graph where every level uses the next type twice stays
    ///     linear (with inlined copies, and every key's case-insensitive twin, it grew 4x per level).
    /// </summary>
    [Test]
    public async Task Keeps_deep_type_graphs_linear()
    {
        var levels = string.Join(
            "\n",
            Enumerable.Range(1, 12).Select(z => $"public class Level{z} {{ public Level{z + 1} First {{ get; set; }} = new(); public Level{z + 1} Second {{ get; set; }} = new(); }}")
        );
        var result = await WithAnnotations()
                          .AddSources(
                               Usings
                             + "namespace Sample;\n"
                             + "[Optionary] public class DeepOptions { public Level1 Root { get; set; } = new(); }\n"
                             + levels
                             + "\npublic class Level13 { public int Value { get; set; } }\npublic class Node<T> { public Node<Node<T>>? Next { get; set; } }\n"
                             + "[Optionary] public class EndlessOptions { public Node<int>? Start { get; set; } }\n"
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        // The binder stops at a generic nesting depth as well, and what it generates still compiles.
        await result.FinalDiagnostics.Where(z => z.Severity == DiagnosticSeverity.Error).Should().BeEmpty();
        var fragments = Fragments(result);
        using var deep = JsonDocument.Parse(fragments["Deep"]);
        await deep.RootElement.GetProperty("definitions").EnumerateObject().Count().Should().BeEqualTo(13);
        await fragments["Deep"].Length.Should().BeLessThan(10_000);
        await result.Results[typeof(ConfigurationGenerator)].Diagnostics.Select(z => z.Id).Should().BeEquivalentTo(["OPT0007"], CollectionOrdering.Matching);
    }

    /// <summary>
    ///     The pipeline's values compare by content, so an unrelated edit leaves the sections cached instead of rebuilding and
    ///     re-emitting them (the reason the model holds no symbols or locations).
    /// </summary>
    [Test]
    public async Task Keeps_sections_cached_when_unrelated_code_changes()
    {
        var result = await WithAnnotations()
                          .AddSources(
                               Usings
                             + """
                               namespace Sample;

                               /// <summary>Settings for the greeting service.</summary>
                               [Optionary]
                               public class GreetingsOptions
                               {
                                   public string Message { get; set; } = "Hello";
                                   public RetryOptions Retry { get; set; } = new();
                               }

                               public class RetryOptions { public int Attempts { get; set; } }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        var compilation = result.InputCompilation;
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees.First().Options;
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new ConfigurationGenerator().AsSourceGenerator()],
            parseOptions: parseOptions,
            driverOptions: new(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true)
        );
        driver = driver.RunGenerators(compilation, TestContext.CancellationToken);
        driver = driver.RunGenerators(
            compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("namespace Sample; public class Unrelated { }", parseOptions, cancellationToken: TestContext.CancellationToken)),
            TestContext.CancellationToken
        );

        var steps = driver.GetRunResult().Results.Single().TrackedSteps;
        foreach (var name in new[] { "optionary:classes", "optionary:sections" })
        {
            var reasons = new List<IncrementalStepRunReason>();
            foreach (var step in steps[name])
            {
                foreach (var (_, reason) in step.Outputs)
                {
                    reasons.Add(reason);
                }
            }

            await reasons.Should().All(z => z is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged).Because(name);
        }
    }

    [Test]
    [Arguments("Camel")]
    [Arguments("Dash")]
    [Arguments("Underscore")]
    public async Task Spells_keys_in_the_declared_key_style(string style)
    {
        var result = await WithAnnotations()
                          .AddSources(ConfigurationKeyStyleSources.Styled(style))
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(Describe(result));
    }

    [Test]
    public async Task Derives_section_names_in_each_key_style()
    {
        var result = await WithAnnotations()
                          .AddSources(
                               Usings
                             + """
                               namespace Sample;

                               [Optionary]
                               public class GreetingsOptions { public string? Message { get; set; } }

                               [Optionary]
                               public class HttpClientOptions { public string? BaseAddress { get; set; } }

                               [Optionary(KeyStyle = ConfigurationKeyStyle.Camel)]
                               public class HttpClientSettings { public string? BaseAddress { get; set; } }

                               [Optionary(KeyStyle = ConfigurationKeyStyle.Camel)]
                               public class OpenAIConfig { public string? ApiKey { get; set; } }

                               [Optionary(KeyStyle = ConfigurationKeyStyle.Dash)]
                               public class HttpClientConfiguration { public string? BaseAddress { get; set; } }

                               [Optionary("Http:RetryPolicy", KeyStyle = ConfigurationKeyStyle.Dash)]
                               public class ExplicitSectionOptions { public int MaxAttempts { get; set; } }

                               [Optionary(KeyStyle = ConfigurationKeyStyle.Dash)]
                               public class OpenAIOptions { public string? ApiKey { get; set; } }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(Describe(result));
    }

    [Test]
    public async Task Styles_the_assembly_form_like_the_class_form()
    {
        var result = await WithAnnotations()
                          .AddSources(
                               Usings
                             + """
                               [assembly: Optionary<Sample.ThirdPartyOptions>(KeyStyle = ConfigurationKeyStyle.Dash)]
                               [assembly: Optionary<Sample.CamelThirdPartyOptions>("Third:Camel", KeyStyle = ConfigurationKeyStyle.Camel)]

                               namespace Sample;

                               public class ThirdPartyOptions
                               {
                                   public string ConnectionString { get; set; } = "";
                                   public ThirdPartyPool PoolSettings { get; set; } = new();
                               }

                               public class ThirdPartyPool { public int MaxSize { get; set; } }

                               public class CamelThirdPartyOptions { public int RetryCount { get; set; } }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(Describe(result));
    }

    [Test]
    public async Task Treats_the_undefined_and_pascal_styles_alike_for_ordinary_names()
    {
        const string members = """
            public string Message { get; set; } = "";
            public int RetryCount { get; set; }
            public Nested Inner { get; set; } = new();
            """;
        var result = await WithAnnotations()
                          .AddSources(
                               Usings
                             + $$"""
                                 namespace Sample;

                                 [Optionary("Default", KeyStyle = ConfigurationKeyStyle.Undefined)]
                                 public class DefaultOptions { {{members}} }

                                 [Optionary("Explicit", KeyStyle = ConfigurationKeyStyle.Pascal)]
                                 public class ExplicitOptions { {{members}} }

                                 public class Nested { public int WaitTime { get; set; } }
                                 """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await result.FinalDiagnostics.Where(z => z.Severity == DiagnosticSeverity.Error && z.Id.StartsWith("CS", StringComparison.Ordinal)).Should().BeEmpty();
        var fragments = Fragments(result);
        // Ordinary C# names are Pascal case already, so Undefined and Pascal spell the same keys. That is also why the Nested type
        // they share is not a conflict: the binder reads the same keys either way.
        await result.Results[typeof(ConfigurationGenerator)].Diagnostics.Should().BeEmpty();
        await fragments["Explicit"].Should().BeEqualTo(fragments["Default"].Replace("\"title\":\"Default\"", "\"title\":\"Explicit\"", StringComparison.Ordinal));
        await fragments["Default"].Should().Contain("\"RetryCount\"");
        await fragments["Default"].Should().Contain("\"WaitTime\"");
    }

    [Test]
    public async Task Respells_only_for_pascal_when_the_declared_name_starts_in_lower_case()
    {
        var result = await WithAnnotations()
                          .AddSources(ConfigurationKeyStyleSources.LowerCaseNames)
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(Describe(result));
    }

    [Test]
    public async Task Rejects_a_type_that_undefined_and_pascal_spell_differently()
    {
        var result = await WithAnnotations()
                          .AddSources(
                               Usings
                             + """
                               namespace Sample;

                               [Optionary("First")]
                               public class FirstOptions { public LowerNested Inner { get; set; } = new(); }

                               [Optionary("Second", KeyStyle = ConfigurationKeyStyle.Pascal)]
                               public class SecondOptions { public LowerNested Inner { get; set; } = new(); }

                               public class LowerNested { public int waitTime { get; set; } }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await result.FinalDiagnostics.Where(z => z.Severity == DiagnosticSeverity.Error && z.Id.StartsWith("CS", StringComparison.Ordinal)).Should().BeEmpty();
        var diagnostics = result.Results[typeof(ConfigurationGenerator)].Diagnostics;
        await diagnostics.Select(z => z.Id).Should().BeEquivalentTo(["OPT0011"], CollectionOrdering.Matching);
        await Fragments(result).Keys.Should().BeEquivalentTo(["First"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Derives_underscore_section_names()
    {
        var result = await WithAnnotations()
                          .AddSources(
                               Usings
                             + """
                               namespace Sample;

                               [Optionary(KeyStyle = ConfigurationKeyStyle.Underscore)]
                               public class HttpClientOptions { public int MaxConnections { get; set; } }

                               [Optionary("Explicit:Section", KeyStyle = ConfigurationKeyStyle.Underscore)]
                               public class ExplicitOptions { public int MaxConnections { get; set; } }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await result.FinalDiagnostics.Where(z => z.Severity == DiagnosticSeverity.Error && z.Id.StartsWith("CS", StringComparison.Ordinal)).Should().BeEmpty();
        await Fragments(result).Keys.Should().BeEquivalentTo(["http_client", "Explicit:Section"]);
        await Fragments(result)["http_client"].Should().Contain("\"max_connections\"");
    }

    [Test]
    public async Task Rejects_a_nested_type_bound_with_two_key_styles()
    {
        var result = await WithAnnotations()
                          .AddSources(
                               Usings
                             + """
                               namespace Sample;

                               [Optionary("First", KeyStyle = ConfigurationKeyStyle.Camel)]
                               public class FirstOptions { public RetryOptions Retry { get; set; } = new(); }

                               [Optionary("Second", KeyStyle = ConfigurationKeyStyle.Dash)]
                               public class SecondOptions { public RetryOptions Retry { get; set; } = new(); }

                               [Optionary("Third", KeyStyle = ConfigurationKeyStyle.Camel)]
                               public class ThirdOptions { public RetryOptions Retry { get; set; } = new(); }

                               public class RetryOptions { public int MaxAttempts { get; set; } }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await result.FinalDiagnostics.Where(z => z.Severity == DiagnosticSeverity.Error && z.Id.StartsWith("CS", StringComparison.Ordinal)).Should().BeEmpty();
        var diagnostics = result.Results[typeof(ConfigurationGenerator)].Diagnostics;
        await diagnostics.Select(z => z.Id).Should().BeEquivalentTo(["OPT0011"], CollectionOrdering.Matching);
        await diagnostics[0].Severity.Should().BeEqualTo(DiagnosticSeverity.Error);
        await diagnostics[0].GetMessage(CultureInfo.InvariantCulture).Should().BeEqualTo("'Sample.RetryOptions' is bound with more than one key style; use the same KeyStyle for every section that reaches it, or give the shared type [ConfigurationKeyName] attributes");
        // The later declaration is the one reported, and it produces nothing; the earlier ones, and a later one in the same style, still do.
        await diagnostics[0].Location.SourceTree!.ToString().Substring(diagnostics[0].Location.SourceSpan.Start, diagnostics[0].Location.SourceSpan.Length).Should().Contain("\"Second\"");
        await Fragments(result).Keys.Should().BeEquivalentTo(["First", "Third"]);
        await result.Results[typeof(ConfigurationGenerator)].SyntaxTrees.Select(z => Path.GetFileName(z.FilePath)).Should().Contain("Optionary.Options.g.cs");
    }

    [Test]
    public async Task Allows_a_shared_type_when_its_keys_do_not_depend_on_the_style()
    {
        var result = await WithAnnotations()
                          .AddSources(
                               Usings
                             + """
                               namespace Sample;

                               [Optionary("A", KeyStyle = ConfigurationKeyStyle.Camel)]
                               public class AOptions { public NamedRetry Retry { get; set; } = new(); public SharedRetry Shared { get; set; } = new(); }

                               [Optionary("B", KeyStyle = ConfigurationKeyStyle.Dash)]
                               public class BOptions { public NamedRetry Retry { get; set; } = new(); }

                               // Described only: nothing is bound, so there is no binder to disagree with.
                               [Optionary("C", KeyStyle = ConfigurationKeyStyle.Dash, RegisterOptions = false)]
                               public class COptions { public SharedRetry Shared { get; set; } = new(); }

                               public class NamedRetry
                               {
                                   [ConfigurationKeyName("max_attempts")]
                                   public int MaxAttempts { get; set; }
                               }

                               public class SharedRetry { public int MaxAttempts { get; set; } }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await result.FinalDiagnostics.Where(z => z.Severity == DiagnosticSeverity.Error).Should().BeEmpty();
        await result.Results[typeof(ConfigurationGenerator)].Diagnostics.Should().BeEmpty();
        await Fragments(result).Keys.Should().BeEquivalentTo(["A", "B", "C"]);
    }

    /// <summary>
    ///     Each ZeroAlloc.Validation rule JSON Schema has a keyword for, next to the data annotation that says the same. The two
    ///     types get the same keywords, apart from <c>pattern</c>: <c>[RegularExpression]</c> matches the whole value and
    ///     <c>[Matches]</c> searches it, as <c>pattern</c> does.
    /// </summary>
    [Test]
    public async Task Describes_ZeroAlloc_Validation_rules_like_data_annotations()
    {
        var result = await WithAnnotations()
                          .AddSources(
                               ZeroAllocValidation,
                               Usings
                             + """
                               using ZeroAlloc.Validation;

                               namespace Sample;

                               public enum LogLevel { Quiet, Normal, Loud }

                               [Optionary, Validate]
                               public class ZeroAllocOptions
                               {
                                   [InclusiveBetween(1, 65535)]
                                   public int? Port { get; set; }

                                   [ExclusiveBetween(0, 1)]
                                   public double Ratio { get; set; }

                                   [GreaterThan(0)]
                                   public int Workers { get; set; }

                                   [GreaterThanOrEqualTo(0.5)]
                                   public decimal Weight { get; set; }

                                   [LessThan(100)]
                                   public int Percent { get; set; }

                                   [LessThanOrEqualTo(10)]
                                   public long Retries { get; set; }

                                   [ZeroAlloc.Validation.MinLength(2)]
                                   public string Name { get; set; } = "";

                                   [ZeroAlloc.Validation.MaxLength(3)]
                                   public string[] Tags { get; set; } = [];

                                   [ZeroAlloc.Validation.Length(2, 8)]
                                   public string Code { get; set; } = "";

                                   [NotEmpty]
                                   public string Title { get; set; } = "";

                                   [NotEmpty]
                                   public List<string> Hosts { get; set; } = [];

                                   [Matches("^[a-z]{2}-[A-Z]{2}$")]
                                   public string Culture { get; set; } = "en-US";

                                   [ZeroAlloc.Validation.EmailAddress]
                                   public string? ReplyTo { get; set; }

                                   [IsInEnum]
                                   public LogLevel Level { get; set; }

                                   [IsEnumName(typeof(LogLevel))]
                                   public string LevelName { get; set; } = "Normal";
                               }

                               [Optionary]
                               public class AnnotatedOptions
                               {
                                   [Range(1, 65535)]
                                   public int? Port { get; set; }

                                   [Range(0d, 1d, MinimumIsExclusive = true, MaximumIsExclusive = true)]
                                   public double Ratio { get; set; }

                                   [System.ComponentModel.DataAnnotations.MinLength(2)]
                                   public string Name { get; set; } = "";

                                   [System.ComponentModel.DataAnnotations.MaxLength(3)]
                                   public string[] Tags { get; set; } = [];

                                   [System.ComponentModel.DataAnnotations.Length(2, 8)]
                                   public string Code { get; set; } = "";

                                   [RegularExpression("[a-z]{2}-[A-Z]{2}")]
                                   public string Culture { get; set; } = "en-US";

                                   [System.ComponentModel.DataAnnotations.EmailAddress]
                                   public string? ReplyTo { get; set; }

                                   public LogLevel Level { get; set; }
                               }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(Describe(result));
    }

    /// <summary>
    ///     A member that states a rule with both libraries gets each keyword once, whatever the order of the attributes. Where the
    ///     two disagree, both run, so the schema takes the tighter bound. The type's own bounds (a byte's 0 to 255) count too.
    /// </summary>
    [Test]
    public async Task Merges_ZeroAlloc_Validation_rules_with_data_annotations()
    {
        var result = await WithAnnotations()
                          .AddSources(
                               ZeroAllocValidation,
                               Usings
                             + """
                               using ZeroAlloc.Validation;

                               namespace Sample;

                               [Optionary, Validate]
                               public class MergedOptions
                               {
                                   [InclusiveBetween(1, 65535), Range(1, 65535)]
                                   public int? Port { get; set; }

                                   [Range(1, 10), InclusiveBetween(5, 20)]
                                   public int RangeFirst { get; set; }

                                   [InclusiveBetween(5, 20), Range(1, 10)]
                                   public int RuleFirst { get; set; }

                                   [GreaterThan(0), LessThanOrEqualTo(100)]
                                   public int TwoRules { get; set; }

                                   [GreaterThan(1), Range(1, 10)]
                                   public int ExclusiveAtTheSameBound { get; set; }

                                   [GreaterThanOrEqualTo(-5), LessThan(1000)]
                                   public byte InsideItsType { get; set; }

                                   [NotEmpty, StringLength(20, MinimumLength = 3)]
                                   public string Name { get; set; } = "";

                                   [ZeroAlloc.Validation.MaxLength(5), System.ComponentModel.DataAnnotations.MaxLength(3), NotEmpty]
                                   public string[] Tags { get; set; } = [];

                                   [Matches("^[a-z]+$"), RegularExpression("[a-z]+")]
                                   public string SamePattern { get; set; } = "";

                                   [ZeroAlloc.Validation.EmailAddress, System.ComponentModel.DataAnnotations.EmailAddress]
                                   public string? ReplyTo { get; set; }
                               }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(Describe(result));
    }

    /// <summary>
    ///     Rules without a keyword, and rules that don't always fail the options (conditional ones, and warnings), are left out of
    ///     the schema without a word. A rule that has a keyword but can't use it here is reported with OPT0008, like a data
    ///     annotation.
    /// </summary>
    [Test]
    public async Task Leaves_out_the_ZeroAlloc_Validation_rules_a_schema_cannot_state()
    {
        var result = await WithAnnotations()
                          .AddSources(
                               ZeroAllocValidation,
                               Usings
                             + """
                               using ZeroAlloc.Validation;

                               namespace Sample;

                               [Optionary, Validate]
                               public class UnstatedOptions
                               {
                                   [Must(nameof(IsEven))]
                                   public int Even { get; set; }

                                   [NotEqual(0)]
                                   public int NotZero { get; set; }

                                   [Equal("yes")]
                                   public string Answer { get; set; } = "yes";

                                   [PrecisionScale(5, 2)]
                                   public decimal Price { get; set; }

                                   [NotEmpty]
                                   public Guid Id { get; set; }

                                   [NotNull, Null, Empty]
                                   public string? Nothing { get; set; }

                                   [GreaterThan(0, When = nameof(Enabled))]
                                   public int WhenEnabled { get; set; }

                                   [GreaterThan(0, Unless = nameof(Enabled))]
                                   public int UnlessEnabled { get; set; }

                                   [GreaterThan(0, Severity = Severity.Warning)]
                                   public int OnlyAWarning { get; set; }

                                   [GreaterThan(0, Severity = Severity.Error, Message = "Has to be positive.", ErrorCode = "E1")]
                                   public int AnError { get; set; }

                                   public bool Enabled { get; set; }

                                   private static bool IsEven(int value) => value % 2 == 0;
                               }

                               [Optionary, Validate, SkipWhen(nameof(Disabled))]
                               public class SkippedOptions
                               {
                                   [GreaterThan(0)]
                                   public int Count { get; set; }

                                   public bool Disabled { get; set; }
                               }

                               [Optionary, Validate]
                               public class ReportedOptions
                               {
                                   [GreaterThan(0)]
                                   public string NotANumber { get; set; } = "";

                                   [ZeroAlloc.Validation.MinLength(1)]
                                   public Dictionary<string, string> NotACollection { get; set; } = [];

                                   [Matches("[a-z]+")]
                                   public int NotAString { get; set; }

                                   [Matches(@"\p{L}+")]
                                   public string NetOnlySyntax { get; set; } = "";

                                   [Matches("[0-9]+"), RegularExpression("[a-z]+")]
                                   public string TwoPatterns { get; set; } = "";
                               }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(Describe(result));
    }

    /// <summary>ZeroAlloc.Validation's attributes, with the constructors and properties of its 2.6 package.</summary>
    private const string ZeroAllocValidation = """
        namespace ZeroAlloc.Validation
        {
            using System;

            public enum Severity { Error, Warning, Info }

            public enum TextLengthUnit { CodeUnits, CodePoints }

            [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
            public sealed class ValidateAttribute : Attribute;

            [AttributeUsage(AttributeTargets.Class)]
            public sealed class SkipWhenAttribute(string methodName) : Attribute
            {
                public string MethodName { get; } = methodName;
            }

            [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
            public abstract class ValidationAttribute : Attribute
            {
                public string? Message { get; set; }
                public string? When { get; set; }
                public string? Unless { get; set; }
                public string? ErrorCode { get; set; }
                public Severity Severity { get; set; }
            }

            public sealed class InclusiveBetweenAttribute(double min, double max) : ValidationAttribute { public double Min { get; } = min; public double Max { get; } = max; }
            public sealed class ExclusiveBetweenAttribute(double min, double max) : ValidationAttribute { public double Min { get; } = min; public double Max { get; } = max; }
            public sealed class GreaterThanAttribute(double value) : ValidationAttribute { public double Value { get; } = value; }
            public sealed class GreaterThanOrEqualToAttribute(double value) : ValidationAttribute { public double Value { get; } = value; }
            public sealed class LessThanAttribute(double value) : ValidationAttribute { public double Value { get; } = value; }
            public sealed class LessThanOrEqualToAttribute(double value) : ValidationAttribute { public double Value { get; } = value; }
            public sealed class MinLengthAttribute(int min) : ValidationAttribute { public int Min { get; } = min; public TextLengthUnit Unit { get; set; } }
            public sealed class MaxLengthAttribute(int max) : ValidationAttribute { public int Max { get; } = max; public TextLengthUnit Unit { get; set; } }
            public sealed class LengthAttribute(int min, int max) : ValidationAttribute { public int Min { get; } = min; public int Max { get; } = max; public TextLengthUnit Unit { get; set; } }
            public sealed class MatchesAttribute(string pattern) : ValidationAttribute { public string Pattern { get; } = pattern; }
            public sealed class IsEnumNameAttribute(Type enumType) : ValidationAttribute { public Type EnumType { get; } = enumType; }
            public sealed class MustAttribute(string methodName) : ValidationAttribute { public string MethodName { get; } = methodName; }
            public sealed class PrecisionScaleAttribute(int precision, int scale) : ValidationAttribute { public int Precision { get; } = precision; public int Scale { get; } = scale; }
            public sealed class EmailAddressAttribute : ValidationAttribute;
            public sealed class NotEmptyAttribute : ValidationAttribute;
            public sealed class EmptyAttribute : ValidationAttribute;
            public sealed class NotNullAttribute : ValidationAttribute;
            public sealed class NullAttribute : ValidationAttribute;
            public sealed class IsInEnumAttribute : ValidationAttribute;

            public sealed class EqualAttribute : ValidationAttribute
            {
                public EqualAttribute(double value) => NumericValue = value;
                public EqualAttribute(string value) => StringValue = value;
                public double NumericValue { get; }
                public string? StringValue { get; }
            }

            public sealed class NotEqualAttribute : ValidationAttribute
            {
                public NotEqualAttribute(double value) => NumericValue = value;
                public NotEqualAttribute(string value) => StringValue = value;
                public double NumericValue { get; }
                public string? StringValue { get; }
            }
        }
        """;

    /// <summary>The raw schema fragments in the compilation, by section.</summary>
    private static Dictionary<string, string> Fragments(GeneratorTestResults result)
    {
        const string prefix = "Optionary.Schema.v1:";
        return result.FinalCompilation.Assembly.GetAttributes()
                     .Select(z => z is { AttributeClass.Name: "AssemblyMetadataAttribute", ConstructorArguments: [{ Value: string key }, { Value: string value }] } ? (key, value) : default)
                     .Where(z => z.key?.StartsWith(prefix, StringComparison.Ordinal) == true)
                     .ToDictionary(z => z.key[prefix.Length..], z => z.value);
    }

    /// <summary>Every schema fragment (pretty-printed JSON), then the generator's diagnostics; the generated code must compile.</summary>
    private static string Describe(GeneratorTestResults result)
    {
        // The generator's own DT errors are part of what's verified below; only compiler errors mean broken generated code.
        var compileErrors = result.FinalDiagnostics.Where(z => z.Severity == DiagnosticSeverity.Error && z.Id.StartsWith("CS", StringComparison.Ordinal)).ToArray();
        if (compileErrors.Length > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, compileErrors.Select(z => z.ToString())));

        const string prefix = "Optionary.Schema.v1:";
        var options = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        var text = new StringBuilder();
        foreach (var attribute in result.FinalCompilation.Assembly.GetAttributes())
        {
            if (attribute is { AttributeClass.Name: "AssemblyMetadataAttribute", ConstructorArguments: [{ Value: string key }, { Value: string value }] }
             && key.StartsWith(prefix, StringComparison.Ordinal))
            {
                using var document = JsonDocument.Parse(value);
                text.AppendLine("## " + key[prefix.Length..]);
                text.AppendLine(JsonSerializer.Serialize(document.RootElement, options));
            }
        }

        text.AppendLine("## Diagnostics");
        foreach (var diagnostic in result.Results[typeof(ConfigurationGenerator)].Diagnostics.OrderBy(z => z.Location.SourceSpan.Start).ThenBy(z => z.Id, StringComparer.Ordinal))
        {
            text.AppendLine($"{diagnostic.Id} {diagnostic.Severity} line {diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1}: {diagnostic.GetMessage(CultureInfo.InvariantCulture)}");
        }

        return text.ToString();
    }
}

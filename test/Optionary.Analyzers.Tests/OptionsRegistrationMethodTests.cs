using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Options;
using Rocket.Surgery.Extensions.Testing.SourceGenerators;

namespace Optionary.Analyzers.Tests;

/// <summary>
///     The registration method: the generator emits
///     <c>OptionaryConfigurationOptions.AddOptionaryConfigurationOptions</c> in the <c>RootNamespace</c> build property, or the global
///     namespace when there is none.
/// </summary>
public class OptionsRegistrationMethodTests() : GeneratorTest()
{
    private const string Usings = """
        using Optionary;

        """;

    private const string OptionsClass = Usings
      + """
        namespace Sample;

        [Optionary]
        public class GreetingsOptions
        {
            public string Message { get; set; } = "Hello";
        }
        """;

    [Test]
    public async Task Emits_the_registration_method_in_the_root_namespace()
    {
        var result = await Options()
                          .AddGlobalOption("build_property.RootNamespace", "Sample.App")
                          .AddSources(OptionsClass)
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(result);
    }

    [Test]
    public async Task Emits_the_registration_method_in_the_global_namespace_without_a_root_namespace()
    {
        var result = await Options().AddSources(OptionsClass).Build().GenerateAsync(TestContext.CancellationToken);

        await Verify(result);
    }

    [Test]
    public async Task Emits_an_empty_registration_method_when_no_section_registers_options()
    {
        var result = await Options()
                          .AddGlobalOption("build_property.RootNamespace", "Sample")
                          .AddSources(
                               Usings
                             + """
                               namespace Sample;

                               [Optionary(RegisterOptions = false)]
                               public class SchemaOnlyOptions
                               {
                                   public string? Value { get; set; }
                               }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(result);
    }

    [Test]
    public async Task Emits_an_empty_registration_method_when_every_declaration_is_rejected()
    {
        var result = await Options()
                          .AddGlobalOption("build_property.RootNamespace", "Sample")
                          .AddSources(
                               Usings
                             + """
                               namespace Sample;

                               [Optionary]
                               public class RefOptions
                               {
                                   public RefOptions(ref int value) => Value = value;

                                   public int Value { get; }
                               }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(result);
    }

    [Test]
    public async Task Emits_an_empty_registration_method_without_declarations()
    {
        var result = await Options()
                          .AddGlobalOption("build_property.RootNamespace", "Sample")
                          .AddSources("namespace Sample; public class PlainOptions { public string Value { get; set; } = \"\"; }")
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(result);
    }

    [Test]
    public async Task Emits_an_empty_registration_method_below_CSharp12()
    {
        var result = await BelowCSharp12();

        await Verify(result);
    }

    [Test]
    public async Task Reports_OPT0001_below_CSharp12()
    {
        var result = await BelowCSharp12();

        await Verify(result.FinalDiagnostics);
    }

    [Test]
    public async Task Emits_an_empty_registration_method_that_compiles_as_CSharp7_3()
    {
        // No #nullable (C# 8) and no file-scoped namespace (C# 10) in what is emitted.
        var result = await Options()
                          .AddGlobalOption("build_property.RootNamespace", "Sample")
                          .WithLanguageVersion(LanguageVersion.CSharp7_3)
                          .AddSources(
                               """
                               namespace Sample
                               {
                                   [Optionary.Optionary]
                                   public class GreetingsOptions
                                   {
                                       public string Message { get; set; } = "Hello";
                                   }
                               }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(result);
    }

    [Test]
    public async Task Emits_the_registration_method_in_the_global_namespace_when_the_root_namespace_is_whitespace()
    {
        var result = await Options()
                          .AddGlobalOption("build_property.RootNamespace", "  ")
                          .AddSources(OptionsClass)
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(result);
    }

    [Test]
    public async Task Emits_the_registration_method_in_the_root_namespace_verbatim()
    {
        // Not a namespace any type is declared in, and not derived from the assembly name: the property is used as written.
        var result = await Options()
                          .AddGlobalOption("build_property.RootNamespace", "Contoso.Shipping_Service.V2")
                          .AddSources(OptionsClass)
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(result);
    }

    [Test]
    public async Task Emits_nothing_for_an_OptionaryAttribute_from_another_assembly()
    {
        var result = await WithoutOptionary()
                          .AddReferences(typeof(Microsoft.Extensions.Configuration.IConfiguration))
                          .AddGlobalOption("build_property.RootNamespace", "Sample")
                          .AddSources(
                               """
                               namespace Optionary
                               {
                                   [System.AttributeUsage(System.AttributeTargets.Class)]
                                   public sealed class OptionaryAttribute : System.Attribute
                                   {
                                       public OptionaryAttribute() { }
                                       public OptionaryAttribute(string section) { }
                                       public bool RegisterOptions { get; set; } = true;
                                   }
                               }

                               namespace Sample
                               {
                                   [Optionary.Optionary]
                                   public class GreetingsOptions
                                   {
                                       public string Message { get; set; } = "Hello";
                                   }
                               }
                               """
                           )
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(result);
    }

    /// <summary>
    ///     Another reference defines <c>Optionary.OptionaryAttribute</c> too (internal there, so the source still binds to
    ///     Optionary's). <c>Compilation.GetTypeByMetadataName</c> returns null for a name two references define, so the guard has to
    ///     pick the one from the assembly named Optionary: the method is emitted and the type is registered.
    /// </summary>
    [Test]
    public async Task Emits_the_registration_method_when_another_reference_also_defines_OptionaryAttribute()
    {
        var result = await Options()
                          .AddReferences(DuplicateOptionaryAttributeReference())
                          .AddGlobalOption("build_property.RootNamespace", "Sample")
                          .AddSources(OptionsClass)
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(result);
    }

    [Test]
    public async Task Emits_nothing_without_Optionary()
    {
        var result = await WithoutOptionary()
                          .AddGlobalOption("build_property.RootNamespace", "Sample")
                          .AddSources("namespace Sample; public class PlainOptions { public string Value { get; set; } = \"\"; }")
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(result);
    }

    /// <summary>An assembly named <c>Other</c> that defines an internal <c>Optionary.OptionaryAttribute</c> of its own.</summary>
    private static MetadataReference DuplicateOptionaryAttributeReference()
    {
        var compilation = CSharpCompilation.Create(
            "Other",
            [
                CSharpSyntaxTree.ParseText(
                    """
                    namespace Optionary
                    {
                        [System.AttributeUsage(System.AttributeTargets.Class)]
                        internal sealed class OptionaryAttribute : System.Attribute { }
                    }
                    """
                ),
            ],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new(OutputKind.DynamicallyLinkedLibrary)
        );
        using var image = new MemoryStream();
        var emitted = compilation.Emit(image);
        return !emitted.Success
            ? throw new InvalidOperationException(string.Join(Environment.NewLine, emitted.Diagnostics.Select(static diagnostic => diagnostic.ToString())))
            : (MetadataReference)MetadataReference.CreateFromImage(image.ToArray());
    }

    private Task<GeneratorTestResults> BelowCSharp12() => Options()
                                                         .AddGlobalOption("build_property.RootNamespace", "Sample")
                                                         .WithLanguageVersion(LanguageVersion.CSharp11)
                                                         .AddSources(OptionsClass)
                                                         .Build()
                                                         .GenerateAsync(TestContext.CancellationToken);

    /// <summary>Optionary's generator with the assemblies its output uses, over a compilation that references the Optionary runtime.</summary>
    private GeneratorTestContextBuilder Options() => WithOptionsReferences(ConfigurationBuilder);

    /// <summary>The same, over a compilation that doesn't reference the Optionary runtime.</summary>
    private static GeneratorTestContextBuilder WithoutOptionary() => WithOptionsReferences(GeneratorTestContextBuilder.Create().WithSdkDefaultNoWarn().WithGenerator<ConfigurationGenerator>());

    private static GeneratorTestContextBuilder WithOptionsReferences(GeneratorTestContextBuilder builder) => builder
                                                                                                           .AddConfigurationOptionsReferences()
                                                                                                           .AddReferences(typeof(IValidateOptions<>))
                                                                                                           .WithDiagnosticSeverity(DiagnosticSeverity.Warning);
}

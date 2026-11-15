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
    public async Task Emits_an_empty_registration_method_and_reports_OPT0001_below_CSharp12()
    {
        var result = await Options()
                          .AddGlobalOption("build_property.RootNamespace", "Sample")
                          .WithLanguageVersion(LanguageVersion.CSharp11)
                          .AddSources(OptionsClass)
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(result);
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
        var result = await GeneratorTestContextBuilder
                          .Create()
                          .WithGenerator<ConfigurationGenerator>()
                          .AddConfigurationOptionsReferences()
                          .AddReferences(typeof(IValidateOptions<>), typeof(Microsoft.Extensions.Configuration.IConfiguration))
                          .WithDiagnosticSeverity(DiagnosticSeverity.Warning)
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

    [Test]
    public async Task Emits_nothing_without_Optionary()
    {
        var result = await GeneratorTestContextBuilder
                          .Create()
                          .WithGenerator<ConfigurationGenerator>()
                          .AddConfigurationOptionsReferences()
                          .WithDiagnosticSeverity(DiagnosticSeverity.Warning)
                          .AddGlobalOption("build_property.RootNamespace", "Sample")
                          .AddSources("namespace Sample; public class PlainOptions { public string Value { get; set; } = \"\"; }")
                          .Build()
                          .GenerateAsync(TestContext.CancellationToken);

        await Verify(result);
    }

    private GeneratorTestContextBuilder Options() => ConfigurationBuilder
                                                    .AddConfigurationOptionsReferences()
                                                    .AddReferences(typeof(IValidateOptions<>))
                                                    .WithDiagnosticSeverity(DiagnosticSeverity.Warning);
}

using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Rocket.Surgery.Extensions.Testing.SourceGenerators;

namespace Optionary.Analyzers.Tests;

internal static class GeneratorTestContextBuilderExtensions
{
    /// <summary>
    ///     The assemblies the code generated for <c>[Optionary]</c> types uses: the binder and the registration method. Real
    ///     projects reference them through the framework and Optionary's dependencies.
    /// </summary>
    public static GeneratorTestContextBuilder AddConfigurationOptionsReferences(this GeneratorTestContextBuilder builder) => builder
       .AddReferences(
            typeof(ConfigurationSection),
            typeof(Microsoft.Extensions.Options.OptionsBuilder<>),
            typeof(ServiceDescriptor),
            typeof(Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions),
            typeof(Enumerable)
        )
       // System.Linq's ToDictionary returns Dictionary<,>, which it references through the System.Collections facade.
       .AddReferences(System.Reflection.Assembly.Load(new System.Reflection.AssemblyName("System.Collections")));

    public static GeneratorTestContextBuilder AddCommonReferences(this GeneratorTestContextBuilder builder) => builder.AddReferences(
        typeof(ActivatorUtilities),
        typeof(OptionaryAttribute),
        typeof(IServiceProvider),
        typeof(IConfiguration)
        );

    /// <summary>
    ///     Suppresses CS1701 and CS1702 the way the SDK's default <c>NoWarn</c> does. The test host can load a newer
    ///     Microsoft.Extensions assembly than the one Optionary was compiled against, and a real build never reports that.
    /// </summary>
    public static GeneratorTestContextBuilder WithSdkDefaultNoWarn(this GeneratorTestContextBuilder builder) => builder
       .WithDiagnosticOption("CS1701", ReportDiagnostic.Suppress)
       .WithDiagnosticOption("CS1702", ReportDiagnostic.Suppress);
}

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
}

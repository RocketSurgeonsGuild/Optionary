using Rocket.Surgery.Extensions.Testing;
using Rocket.Surgery.Extensions.Testing.SourceGenerators;

namespace Optionary.Analyzers.Tests;

public abstract class GeneratorTest() : LoggerTest<TestRecord>(TestRecord.Create())
{
    /// <summary>Runs Optionary's generator over a compilation that references the Optionary runtime.</summary>
    protected GeneratorTestContextBuilder ConfigurationBuilder { get; } = GeneratorTestContextBuilder
                                                                         .Create()
                                                                         .AddCommonReferences()
                                                                         .WithSdkDefaultNoWarn()
                                                                         .WithGenerator<ConfigurationGenerator>();
}

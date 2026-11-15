using Microsoft.CodeAnalysis;

namespace Optionary.Analyzers;

/// <summary>
///     Describes every <c>[Optionary]</c> section as a JSON Schema fragment and generates the reflection-free options
///     registration for it.
/// </summary>
[Generator]
public sealed class ConfigurationGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context) => ConfigurationSchemaPipeline.Register(context);
}

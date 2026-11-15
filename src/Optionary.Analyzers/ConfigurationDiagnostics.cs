using Microsoft.CodeAnalysis;

namespace Optionary.Analyzers;

/// <summary>The diagnostics <see cref="ConfigurationGenerator" /> reports. Each has an OPT ID and the Optionary category.</summary>
internal static class ConfigurationDiagnostics
{
    /// <summary>
    ///     Below C# 12 the registration method is still emitted, so its callers compile, but it registers nothing: the binder it
    ///     calls uses C# 12 syntax.
    /// </summary>
    public static readonly DiagnosticDescriptor UnsupportedLanguageVersion = new(
        "OPT0001",
        "C# version too old for Optionary source generation",
        "Optionary's source generator needs C# 12 or later, but this project uses C# {0}, so AddOptionaryConfigurationOptions registers nothing and no configuration schema is generated; set LangVersion to 12 or later to enable it",
        "Optionary",
        DiagnosticSeverity.Warning,
        true
    );

    public static readonly DiagnosticDescriptor InvalidConfigurationType = new(
        "OPT0002",
        "Invalid configuration type",
        "'{0}' can't be described as configuration: {1}",
        "Optionary",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor InvalidConfigurationSection = new(
        "OPT0003",
        "Invalid configuration section",
        "'{0}' isn't a valid configuration section for '{1}': {2}",
        "Optionary",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor UnsupportedConfigurationMember = new(
        "OPT0004",
        "Unsupported configuration member",
        "'{0}' has type '{1}', which configuration binding doesn't support, so it's left out of the schema",
        "Optionary",
        DiagnosticSeverity.Warning,
        true
    );

    public static readonly DiagnosticDescriptor ConfigurationKeyCollision = new(
        "OPT0005",
        "Configuration key collision",
        "'{0}' and '{1}' both bind the key '{2}' (configuration keys are case-insensitive), so only '{0}' is described",
        "Optionary",
        DiagnosticSeverity.Warning,
        true
    );

    public static readonly DiagnosticDescriptor UnsupportedConfigurationConstructor = new(
        "OPT0006",
        "Configuration type can't be constructed",
        "'{0}' can't be created from configuration because {1}",
        "Optionary",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor ConfigurationTypeTooLarge = new(
        "OPT0007",
        "Configuration type too large",
        "'{0}' uses more than {1} distinct types, so the schema stops describing them at '{2}'",
        "Optionary",
        DiagnosticSeverity.Warning,
        true
    );

    public static readonly DiagnosticDescriptor InexpressibleConfigurationAnnotation = new(
        "OPT0008",
        "Annotation not expressible in JSON Schema",
        "'{0}' on '{1}' is left out of the schema because {2}",
        "Optionary",
        DiagnosticSeverity.Warning,
        true
    );

    public static readonly DiagnosticDescriptor ConfigurationTypeDeclaredTwice = new(
        "OPT0010",
        "Configuration type declared more than once",
        "'{0}' is configured more than once; a type can have only one [Optionary]",
        "Optionary",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor ConfigurationSchemaFailed = new(
        "OPT0009",
        "Configuration schema generation failed",
        "Optionary couldn't describe '{0}' as configuration ({1}), so it has no schema; please report this",
        "Optionary",
        DiagnosticSeverity.Warning,
        true
    );

    public static readonly DiagnosticDescriptor ConfigurationKeyStyleConflict = new(
        "OPT0011",
        "Configuration key style conflict",
        "'{0}' is bound with more than one key style; use the same KeyStyle for every section that reaches it, or give the shared type [ConfigurationKeyName] attributes",
        "Optionary",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor ZeroAllocAsyncOptionsValidation = new(
        "OPT0012",
        "ZeroAlloc options validation skipped",
        "'{0}' isn't validated with ZeroAlloc.Validation when its options are read, because '{1}' has an asynchronous rule and options validation is synchronous",
        "Optionary",
        DiagnosticSeverity.Warning,
        true
    );
}

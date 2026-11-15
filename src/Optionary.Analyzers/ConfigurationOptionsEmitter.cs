using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Optionary.Analyzers.Binding;

namespace Optionary.Analyzers;

/// <summary>What one <c>[Optionary]</c> section registers with the options pipeline, and the types its binder needs.</summary>
/// <param name="ValidateWithZeroAlloc">Whether ZeroAlloc.Validation.Options generates a <c>ValidateWithZeroAlloc()</c> overload for the type.</param>
internal sealed record ConfigurationOptions(
    string Path,
    bool ValidateOnStart,
    TypeRef Type,
    BinderVersion Version,
    EquatableArray<TypeSpec> Types,
    bool ValidateWithZeroAlloc = false
);

/// <summary>An <c>[OptionsValidator]</c> class and the options types it validates.</summary>
internal sealed record ConfigurationValidator(string TypeName, EquatableArray<string> OptionsTypeNames);

/// <summary>
///     Emits <c>OptionaryConfigurationOptions.AddOptionaryConfigurationOptions(services)</c>, which registers this assembly's
///     <c>[Optionary]</c> types with the options pipeline, and the reflection-free binder it uses. The class is
///     internal and lives in the project's <c>RootNamespace</c> (the global namespace when that is empty), so a library can call
///     it from its own extension method and a host framework (Dovetail's exported joint, for one) can call it by a name it knows without reading this output.
/// </summary>
internal static class ConfigurationOptionsEmitter
{
    public const string ClassName = "OptionaryConfigurationOptions";
    public const string MethodName = "AddOptionaryConfigurationOptions";
    private const string BinderClassName = "OptionaryBinder";

    /// <summary>The generated file. The method is there even when no section registers options, so that its callers always compile.</summary>
    /// <param name="rootNamespace">The <c>RootNamespace</c> build property, used verbatim; empty or whitespace is the global namespace.</param>
    /// <param name="registrations">The sections that register options.</param>
    /// <param name="validators">The assembly's <c>[OptionsValidator]</c> classes.</param>
    /// <param name="zeroAllocExtensions">
    ///     The class ZeroAlloc.Validation.Options generates <c>ValidateWithZeroAlloc()</c> into, or <see langword="null" /> when
    ///     Optionary leaves ZeroAlloc.Validation's registrations to the project.
    /// </param>
    public static string Emit(
        string? rootNamespace,
        EquatableArray<ConfigurationOptions> registrations,
        EquatableArray<ConfigurationValidator> validators,
        string? zeroAllocExtensions = null
    )
    {
        var index = new BindingTypeIndex(registrations.SelectMany(z => z.Types));
        var methods = new BindingMethods(index);
        var roots = new Dictionary<TypeRef, ObjectSpec>();
        foreach (var registration in registrations)
        {
            if (!roots.ContainsKey(registration.Type)
             && index.TryGetTypeSpec(registration.Type, out var spec)
             && spec is ObjectSpec root
             && index.CanInstantiate(root)
             && methods.RegisterRoot(root.TypeRef))
            {
                roots[root.TypeRef] = root;
            }
        }

        var writer = new SourceWriter();
        var hasNamespace = WriteMethodStart(writer, rootNamespace, nullableDirectives: true, out var attributes);
        foreach (var registration in registrations
                                    .Where(z => roots.ContainsKey(z.Type))
                                    .OrderBy(z => z.Path, StringComparer.OrdinalIgnoreCase)
                                    .ThenBy(z => z.Path, StringComparer.Ordinal)
                                    .ThenBy(z => z.Type.FullyQualifiedName, StringComparer.Ordinal))
        {
            writer.WriteLine(Registration(registration, roots[registration.Type], zeroAllocExtensions) + ";");
        }

        foreach (var (options, validator) in validators
                                            .SelectMany(z => z.OptionsTypeNames.Select(options => (options, validator: z.TypeName)))
                                            .Where(z => roots.Keys.Any(root => root.FullyQualifiedName == z.options))
                                            .Distinct()
                                            .OrderBy(z => z.options, StringComparer.Ordinal)
                                            .ThenBy(z => z.validator, StringComparer.Ordinal))
        {
            writer.WriteLine(
                "global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddEnumerable(services, "
              + $"global::Microsoft.Extensions.DependencyInjection.ServiceDescriptor.Singleton<global::Microsoft.Extensions.Options.IValidateOptions<{options}>, {validator}>());"
            );
        }

        WriteMethodEnd(writer);

        if (roots.Count > 0)
        {
            writer.WriteLine();
            WriteBinder(writer, index, methods, registrations[0].Version, roots, attributes);
        }

        WriteNamespaceEnd(writer, hasNamespace);
        return writer.ToString();
    }

    /// <summary>
    ///     The generated file below C# 12: the same method, registering nothing, so that its callers still compile. It uses no
    ///     syntax newer than the project's C# version allows (no <c>#nullable</c> below C# 8).
    /// </summary>
    /// <param name="rootNamespace">The <c>RootNamespace</c> build property, used verbatim; empty or whitespace is the global namespace.</param>
    /// <param name="languageVersion">The project's C# version.</param>
    public static string EmitEmpty(string? rootNamespace, LanguageVersion languageVersion)
    {
        var writer = new SourceWriter();
        var hasNamespace = WriteMethodStart(writer, rootNamespace, languageVersion >= LanguageVersion.CSharp8, out _);
        WriteMethodEnd(writer);
        WriteNamespaceEnd(writer, hasNamespace);
        return writer.ToString();
    }

    /// <summary>Writes the file header, the namespace and the method up to its first statement. Returns whether a namespace was opened.</summary>
    private static bool WriteMethodStart(SourceWriter writer, string? rootNamespace, bool nullableDirectives, out string attributes)
    {
        writer.WriteLine("// <auto-generated/>");
        writer.WriteLine();
        if (nullableDirectives)
        {
            writer.WriteLine("#nullable enable annotations");
            writer.WriteLine("#nullable disable warnings");
            writer.WriteLine();
        }

        writer.WriteLine(
            """
            // Suppress warnings about [Obsolete] member usage in generated code.
            #pragma warning disable CS0612, CS0618

            """
        );

        var hasNamespace = !string.IsNullOrWhiteSpace(rootNamespace);
        if (hasNamespace)
        {
            writer.WriteLine($"namespace {rootNamespace}");
            writer.WriteLine("{");
            writer.Indentation++;
        }

        attributes = ConfigurationHelpers.CompilerGeneratedAttributes.NormalizeWhitespace().ToFullString();
        writer.WriteLine(
            $$"""
            /// <summary>
            ///     Registers this assembly's <c>[Optionary]</c> types with the options pipeline, bound from configuration
            ///     without reflection.
            /// </summary>
            {{attributes}}
            internal static class {{ClassName}}
            {
                /// <summary>
                ///     Registers this assembly's <c>[Optionary]</c> types with the options pipeline. Every registration is a
                ///     <c>TryAdd</c>, so calling it more than once is harmless.
                /// </summary>
                internal static global::Microsoft.Extensions.DependencyInjection.IServiceCollection {{MethodName}}(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)
                {
            """
        );
        writer.Indentation += 2;
        return hasNamespace;
    }

    private static void WriteMethodEnd(SourceWriter writer)
    {
        writer.WriteLine("return services;");
        writer.Indentation -= 2;
        writer.WriteLine(
            """
                }
            }
            """
        );
    }

    private static void WriteNamespaceEnd(SourceWriter writer, bool hasNamespace)
    {
        if (hasNamespace)
        {
            writer.Indentation--;
            writer.WriteLine("}");
        }
    }

    private static void WriteBinder(SourceWriter writer, BindingTypeIndex index, BindingMethods methods, BinderVersion version, Dictionary<TypeRef, ObjectSpec> roots, string attributes)
    {
        writer.WriteLine(
            $"""
            /// <summary>Binds configuration to this assembly's <c>[Optionary]</c> types the way Microsoft's configuration binding source generator (version {( (int)version ).ToString(CultureInfo.InvariantCulture)}) does.</summary>
            {attributes}
            file static class {BinderClassName}
            {"{"}
            """
        );
        writer.Indentation++;
        writer.WriteLine(
            BindingEmitter
               .Emit(
                    index,
                    methods,
                    version,
                    roots.Values.Where(z => z.InstantiationStrategy is not ObjectInstantiationStrategy.ParameterizedConstructor).OrderBy(z => z.TypeRef.FullyQualifiedName, StringComparer.Ordinal),
                    roots.Values.Where(z => z.InstantiationStrategy is ObjectInstantiationStrategy.ParameterizedConstructor).OrderBy(z => z.TypeRef.FullyQualifiedName, StringComparer.Ordinal)
                )
               .TrimEnd('\n')
        );
        writer.Indentation--;
        writer.WriteLine("}");
    }

    private static string Registration(ConfigurationOptions registration, ObjectSpec root, string? zeroAllocExtensions)
    {
        // Every root is created by its generated Create entry point, which also binds types the options pipeline could create.
        var create = $"static configuration => {BinderClassName}.{BindingEmitter.CreateMethodName(root)}(configuration)";
        var call = "global::Optionary.OptionaryServiceCollectionExtensions.AddOptionaryOptions<"
          + registration.Type.FullyQualifiedName
          + ">(services, "
          + SymbolDisplay.FormatLiteral(registration.Path, quote: true)
          + ", "
          + create
          + ")";
        // The overload registers the type's generated validator as its IValidateOptions<T>, with TryAdd.
        if (registration.ValidateWithZeroAlloc && zeroAllocExtensions is not null)
        {
            call = $"{zeroAllocExtensions}.ValidateWithZeroAlloc({call})";
        }

        return registration.ValidateOnStart ? $"global::Microsoft.Extensions.DependencyInjection.OptionsBuilderExtensions.ValidateOnStart({call})" : call;
    }

    /// <summary>The options types an <c>[OptionsValidator]</c> class validates, when it's usable from this assembly's generated code.</summary>
    public static ConfigurationValidator? ReadValidator(INamedTypeSymbol type, Compilation compilation)
    {
        if (type is { IsAbstract: true } or { IsStatic: true } || type.IsOpenGenericType() || !compilation.IsSymbolAccessibleWithin(type, compilation.Assembly))
        {
            return null;
        }

        var options = type
                     .AllInterfaces
                     .Where(z => z is { Name: "IValidateOptions", TypeArguments.Length: 1 } && z.ContainingNamespace.ToDisplayString() == "Microsoft.Extensions.Options")
                     .Select(z => z.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                     .ToEquatableArray();
        return options.IsEmpty ? null : new(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), options);
    }
}

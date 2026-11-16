using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Optionary.Analyzers;

/// <summary>
///     The helpers <see cref="ConfigurationGenerator" /> uses, originally copied from Dovetail's joint generator
///     (<c>Helpers</c> and <c>MsBuildExtensions</c>).
/// </summary>
internal static class ConfigurationHelpers
{
    public static T? GetBuildProperty<T>(this AnalyzerConfigOptions options, string propertyName, Func<string, T?> transform) =>
        options.TryGetValue($"build_property.{propertyName}", out var value) ? transform(value) : default;

    public static CompilationUnitSyntax AddSharedTrivia(this CompilationUnitSyntax source) => source
                                                                                             .WithLeadingTrivia(
                                                                                                  Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true)),
                                                                                                  Trivia(
                                                                                                      PragmaWarningDirectiveTrivia(Token(SyntaxKind.DisableKeyword), true)
                                                                                                         .WithErrorCodes(SeparatedList(List(DisabledWarnings.Value)))
                                                                                                  )
                                                                                              )
                                                                                             .WithTrailingTrivia(
                                                                                                  Trivia(
                                                                                                      PragmaWarningDirectiveTrivia(Token(SyntaxKind.RestoreKeyword), true)
                                                                                                         .WithErrorCodes(SeparatedList(List(DisabledWarnings.Value)))
                                                                                                  ),
                                                                                                  Trivia(NullableDirectiveTrivia(Token(SyntaxKind.RestoreKeyword), true)),
                                                                                                  CarriageReturnLineFeed
                                                                                              );

    public static bool IsOpenGenericType(this INamedTypeSymbol type) =>
        type.IsGenericType && ( type.IsUnboundGenericType || type.TypeArguments.All(z => z.TypeKind == TypeKind.TypeParameter) );

    /// <summary>
    ///     Optionary's name: the package, the runtime assembly the attributes must come from (plan section 4.3), and the tool in
    ///     <c>[GeneratedCode]</c>, which names the package rather than the generator assembly (plan section 4.2).
    /// </summary>
    internal const string OptionaryName = "Optionary";

    /// <summary>The tool name in <c>[GeneratedCode]</c>. Part of the registration method's contract (plan section 4.2).</summary>
    internal const string GeneratedCodeTool = OptionaryName;

    /// <summary>
    ///     <c>[GeneratedCode("Optionary", version)]</c>, <c>[CompilerGenerated]</c> and <c>[ExcludeFromCodeCoverage]</c>, qualified with
    ///     <c>global::</c> because the code they mark sits in the project's <c>RootNamespace</c>, which may itself contain a <c>System</c>.
    /// </summary>
    internal static readonly AttributeListSyntax CompilerGeneratedAttributes =
        AttributeList(
            SeparatedList(
                [
                    Attribute(ParseName("global::System.CodeDom.Compiler.GeneratedCode"))
                       .WithArgumentList(
                            AttributeArgumentList(
                                SeparatedList(
                                    [
                                        AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(GeneratedCodeTool))),
                                        AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(AssemblyInfo.Attributes.FileVersion))),
                                    ]
                                )
                            )
                        ),
                    Attribute(ParseName("global::System.Runtime.CompilerServices.CompilerGenerated")),
                    Attribute(ParseName("global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage")),
                ]
            )
        );

    internal static AttributeListSyntax AddAssemblyAttribute(string key, string? value) => AttributeList(
            SingletonSeparatedList(
                Attribute(QualifiedName(QualifiedName(IdentifierName("System"), IdentifierName("Reflection")), IdentifierName("AssemblyMetadata")))
                   .WithArgumentList(
                        AttributeArgumentList(
                            SeparatedList(
                                [
                                    AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(key))),
                                    AttributeArgument(
                                        value is null
                                            ? LiteralExpression(SyntaxKind.NullLiteralExpression)
                                            : LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(value))
                                    ),
                                ]
                            )
                        )
                    )
            )
        )
       .WithTarget(AttributeTargetSpecifier(Token(SyntaxKind.AssemblyKeyword)));

    private static readonly string[] _disabledWarnings =
    [
        "CA1002",
        "CA1034",
        "CA1822",
        "CS0105",
        "CS1573",
        "CS8602",
        "CS8603",
        "CS8618",
        "CS8669",
    ];

    private static readonly Lazy<ImmutableArray<ExpressionSyntax>> DisabledWarnings = new(
        () => [.. _disabledWarnings.Select(z => (ExpressionSyntax)IdentifierName(z))]
    );
}

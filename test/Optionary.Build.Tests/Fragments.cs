namespace Optionary.Build.Tests;

/// <summary>
///     Hand-written <c>[assembly: AssemblyMetadata("Optionary.Schema.v1:...", ...)]</c> fragments in the generator's shape, so the
///     schema tests depend on the targets and tasks rather than on the generator.
/// </summary>
internal static class Fragments
{
    /// <summary>
    ///     "Greetings", with an object type in the fragment's definitions.
    /// </summary>
    public const string Greetings = """"
        [assembly: System.Reflection.AssemblyMetadata("Optionary.Schema.v1:Greetings", """{"type":"object","title":"Greetings","description":"Options of the greeter.","properties":{"Message":{"type":"string","description":"Text placed before the name."},"Endpoint":{"type":["object","null"],"allOf":[{"$ref":"#/definitions/Endpoint"}]}},"patternProperties":{"^(?:[Mm][Ee][Ss][Ss][Aa][Gg][Ee])$":{"type":"string","description":"Text placed before the name."},"^(?:[Ee][Nn][Dd][Pp][Oo][Ii][Nn][Tt])$":{"type":["object","null"],"allOf":[{"$ref":"#/definitions/Endpoint"}]}},"definitions":{"Endpoint":{"properties":{"Url":{"type":"string"}},"patternProperties":{"^(?:[Uu][Rr][Ll])$":{"type":"string"}}}}}""")]
        """";

    /// <summary>
    ///     "greetings" again (another library binding the same section), and a nested "Shared:Http".
    /// </summary>
    public const string Shared = """"
        [assembly: System.Reflection.AssemblyMetadata("Optionary.Schema.v1:greetings", """{"type":"object","title":"greetings","properties":{"Count":{"type":"integer"}},"patternProperties":{"^(?:[Cc][Oo][Uu][Nn][Tt])$":{"type":"integer"}}}""")]
        [assembly: System.Reflection.AssemblyMetadata("Optionary.Schema.v1:Shared:Http", """{"type":"object","title":"Shared:Http","properties":{"Timeout":{"type":"integer"}},"patternProperties":{"^(?:[Tt][Ii][Mm][Ee][Oo][Uu][Tt])$":{"type":"integer"}}}""")]
        """";

    /// <summary>
    ///     The application's own section.
    /// </summary>
    public const string App = """"
        [assembly: System.Reflection.AssemblyMetadata("Optionary.Schema.v1:App", """{"type":"object","title":"App","properties":{"Name":{"type":"string"}},"patternProperties":{"^(?:[Nn][Aa][Mm][Ee])$":{"type":"string"}}}""")]
        """";

    /// <summary>
    ///     A section whose schema differs per target framework.
    /// </summary>
    public const string PerFramework = """"
        #if NET11_0_OR_GREATER
        [assembly: System.Reflection.AssemblyMetadata("Optionary.Schema.v1:Greetings", """{"type":"object","title":"Greetings","properties":{"Net11":{"type":"string"}}}""")]
        #else
        [assembly: System.Reflection.AssemblyMetadata("Optionary.Schema.v1:Greetings", """{"type":"object","title":"Greetings","properties":{"Net10":{"type":"string"}}}""")]
        #endif
        """";

    public const string Program = "System.Console.WriteLine(\"hello\");";
}

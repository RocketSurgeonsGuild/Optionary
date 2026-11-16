using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Optionary.Samples.Library;

/// <summary>How the sample library greets people.</summary>
/// <remarks>
///     The class form: section <c>Greetings</c>, the type name without its <c>Options</c> suffix. The annotations become
///     <c>minimum</c>, <c>maximum</c> and descriptions in the schema; the validator below enforces the range at run time.
/// </remarks>
[Optionary]
public sealed class GreetingsOptions
{
    /// <summary>The text that comes before the name.</summary>
    [Description("The text that comes before the name, e.g. \"Hello\".")]
    public string Message { get; set; } = "Hello";

    /// <summary>How many times the greeting is said.</summary>
    [Range(1, 5)]
    public int Repeat { get; set; } = 1;

    /// <summary>Who gets greeted.</summary>
    public IList<string> Recipients { get; set; } = [];
}

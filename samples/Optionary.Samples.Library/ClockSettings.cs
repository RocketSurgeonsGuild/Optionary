namespace Optionary.Samples.Library;

/// <summary>Formatting for the time the sample library prints.</summary>
/// <remarks>
///     No attribute here: this stands in for a type you can't put <c>[Optionary]</c> on, such as one from another package. It is
///     declared on the assembly instead (see AssemblyDeclarations.cs).
/// </remarks>
public sealed class ClockSettings
{
    /// <summary>The .NET date and time format string.</summary>
    public string Format { get; set; } = "HH:mm";

    /// <summary>Whether to print the time in UTC rather than local time.</summary>
    public bool Utc { get; set; }
}

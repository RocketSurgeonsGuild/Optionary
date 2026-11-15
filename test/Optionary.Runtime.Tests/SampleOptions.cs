namespace Optionary.Runtime.Tests;

/// <summary>
///     Stand-in for an <c>[Optionary]</c> options class: a settable-property sealed class bound from a configuration
///     section, suitable for re-binding on reload.
/// </summary>
public sealed class SampleOptions
{
    public string Name { get; set; } = "";

    public int Count { get; set; }
}

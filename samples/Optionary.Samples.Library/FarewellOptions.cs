using System.ComponentModel.DataAnnotations;

namespace Optionary.Samples.Library;

/// <summary>How the sample library says goodbye.</summary>
/// <param name="Message">The text of the farewell.</param>
/// <param name="Exclamations">How many exclamation marks end it.</param>
/// <remarks>A record: the generated binder calls its constructor, so the options are immutable.</remarks>
[Optionary]
public sealed record FarewellOptions(string Message = "Goodbye", [property: Range(0, 5)] int Exclamations = 1);

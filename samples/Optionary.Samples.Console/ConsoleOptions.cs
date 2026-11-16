namespace Optionary.Samples.Console;

/// <summary>How the console sample runs.</summary>
/// <remarks>The application's own declaration, next to the library's: the application schema has both.</remarks>
[Optionary]
public sealed class ConsoleOptions
{
    /// <summary>The line printed before the bound values.</summary>
    public string Banner { get; set; } = "Optionary console sample";
}

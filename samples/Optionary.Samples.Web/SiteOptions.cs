using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Optionary.Samples.Web;

/// <summary>The site's own settings.</summary>
/// <remarks>
///     <c>ValidateOnStart = true</c>: with an invalid value the application fails when it starts, not on the first request that
///     reads the options. Try <c>Site__Title=""</c>.
/// </remarks>
[Optionary(ValidateOnStart = true)]
public sealed class SiteOptions
{
    /// <summary>The title shown on the home page.</summary>
    [Required]
    [MinLength(1)]
    public string Title { get; set; } = "";

    /// <summary>How many greetings the home page shows at most.</summary>
    [Range(1, 100)]
    public int MaxGreetings { get; set; } = 10;
}

/// <summary>Enforces <see cref="SiteOptions" />'s data annotations; the generated registration method registers it.</summary>
[OptionsValidator]
public sealed partial class SiteOptionsValidator : IValidateOptions<SiteOptions>;

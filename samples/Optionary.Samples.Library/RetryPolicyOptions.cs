using System.ComponentModel.DataAnnotations;

namespace Optionary.Samples.Library;

/// <summary>How the sample library retries.</summary>
/// <remarks>
///     A non-default key style: with <see cref="ConfigurationKeyStyle.Underscore" /> the section is <c>retry_policy</c> and the
///     keys are <c>max_attempts</c> and <c>delay</c>. <c>MaxAttempts</c> doesn't bind any more.
/// </remarks>
[Optionary(KeyStyle = ConfigurationKeyStyle.Underscore)]
public sealed class RetryPolicyOptions
{
    /// <summary>How many times an operation is tried before it fails.</summary>
    [Range(1, 10)]
    public int MaxAttempts { get; set; } = 3;

    /// <summary>How long to wait between attempts.</summary>
    public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1);
}

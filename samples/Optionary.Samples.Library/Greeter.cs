using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;

namespace Optionary.Samples.Library;

/// <summary>Greets people with the current options; reads them through <see cref="IOptionsMonitor{TOptions}" />, so it sees reloads.</summary>
public sealed class Greeter(
    IOptionsMonitor<GreetingsOptions> greetings,
    IOptionsMonitor<FarewellOptions> farewell,
    IOptions<ClockSettings> clock,
    TimeProvider timeProvider
)
{
    /// <summary>The greetings for every recipient.</summary>
    public IEnumerable<string> Greet()
    {
        var options = greetings.CurrentValue;
        foreach (var recipient in options.Recipients)
        {
            yield return string.Join(' ', Enumerable.Repeat($"{options.Message}, {recipient}!", options.Repeat));
        }
    }

    /// <summary>The farewell, stamped with the time.</summary>
    public string Farewell()
    {
        var options = farewell.CurrentValue;
        var now = clock.Value.Utc ? timeProvider.GetUtcNow() : timeProvider.GetLocalNow();
        return new StringBuilder(options.Message)
              .Append('!', options.Exclamations)
              .Append(" (")
              .Append(now.ToString(clock.Value.Format, CultureInfo.InvariantCulture))
              .Append(')')
              .ToString();
    }
}

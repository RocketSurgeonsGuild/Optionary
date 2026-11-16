using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Optionary.Samples.Console;
using Optionary.Samples.Library;

var builder = Host.CreateApplicationBuilder(args);

// The host's default sources, all from Microsoft's providers: appsettings.json, then appsettings.{Environment}.json (both
// reloaded on change), then environment variables. Later sources win, so DOTNET_ENVIRONMENT=Development applies
// appsettings.Development.json on top, and Greetings__Message=Hi overrides both files.
builder.Services.AddSampleLibrary(); // the library's own registration, which calls its generated AddOptionaryConfigurationOptions()
builder.Services.AddOptionaryConfigurationOptions(); // this application's generated registration method, for ConsoleOptions

using var host = builder.Build();
var services = host.Services;

var lastPrinted = "";
var printLock = new Lock();
Print();

if (!args.Contains("--watch"))
{
    return;
}

// IOptionsMonitor<T> sees reloads: edit an appsettings file while this runs and the values are printed again. A file watcher
// can raise several events for one save, so Print() skips output that hasn't changed.
using var greetingsChanged = services.GetRequiredService<IOptionsMonitor<GreetingsOptions>>().OnChange((_, _) => Print());
using var farewellChanged = services.GetRequiredService<IOptionsMonitor<FarewellOptions>>().OnChange((_, _) => Print());
using var retryChanged = services.GetRequiredService<IOptionsMonitor<RetryPolicyOptions>>().OnChange((_, _) => Print());
Console.WriteLine("Watching the appsettings files for changes. Press Ctrl+C to stop.");
await host.WaitForShutdownAsync();

void Print()
{
    var console = services.GetRequiredService<IOptions<ConsoleOptions>>().Value;
    var greeter = services.GetRequiredService<Greeter>();
    var retry = services.GetRequiredService<IOptionsMonitor<RetryPolicyOptions>>().CurrentValue;
    var clock = services.GetRequiredService<IOptions<ClockSettings>>().Value;

    using var output = new StringWriter();
    output.WriteLine(console.Banner);
    foreach (var greeting in greeter.Greet())
    {
        output.WriteLine($"  {greeting}");
    }

    output.WriteLine($"  {greeter.Farewell()}");
    output.WriteLine($"  retry_policy: max_attempts={retry.MaxAttempts}, delay={retry.Delay}");
    output.WriteLine($"  Samples:Clock: format={clock.Format}, utc={clock.Utc}");

    var text = output.ToString();
    lock (printLock)
    {
        if (text == lastPrinted)
        {
            return;
        }

        lastPrinted = text;
        Console.Write(text);
    }
}

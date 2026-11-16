using Microsoft.Extensions.Options;
using Optionary.Samples.Library;
using Optionary.Samples.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSampleLibrary(); // the library's registration, including its GreetingsOptionsValidator
builder.Services.AddOptionaryConfigurationOptions(); // this application's generated registration: SiteOptions, ValidateOnStart

// The library didn't ask for ValidateOnStart, but the application can: its validator then runs at startup too.
builder.Services.AddOptions<GreetingsOptions>().ValidateOnStart();

var app = builder.Build();

app.MapGet(
    "/",
    (IOptionsSnapshot<SiteOptions> site, Greeter greeter) => new
    {
        site.Value.Title,
        Greetings = greeter.Greet().Take(site.Value.MaxGreetings).ToArray(),
        Farewell = greeter.Farewell(),
    }
);

await app.RunAsync();

using Microsoft.Build.Utilities.ProjectCreation;

namespace Optionary.Build.Tests;

/// <summary>
///     The whole package in a console application, as a user writes it: an <c>[Optionary]</c> options type, the generated
///     <c>AddOptionaryConfigurationOptions()</c> registration, and <c>IOptions&lt;T&gt;</c> bound from configuration, with the
///     type's section in the application's schema.
/// </summary>
[TargetFrameworks]
public class EndToEndTests(string targetFramework)
{
    [Test]
    public async Task ConsoleApp_BindsItsOptionaryOptions_AndDescribesThemInItsSchema()
    {
        using var project = new OptionaryTestProject();
        project
           .AddProject("app/app.csproj", ProjectCreator.Templates.SdkCsproj(targetFramework: targetFramework, outputType: "Exe"))
           .AddFile(
                "app/Program.cs",
                """
                using app;
                using Microsoft.Extensions.Configuration;
                using Microsoft.Extensions.DependencyInjection;
                using Microsoft.Extensions.Options;

                var configuration = new ConfigurationBuilder()
                   .AddInMemoryCollection([new("Greetings:Message", "Hello from configuration"), new("Greetings:Count", "3")])
                   .Build();
                var services = new ServiceCollection().AddSingleton<IConfiguration>(configuration);
                services.AddOptionaryConfigurationOptions();
                using var provider = services.BuildServiceProvider();

                var greetings = provider.GetRequiredService<IOptions<GreetingsOptions>>().Value;
                Console.WriteLine($"{greetings.Message} x{greetings.Count}");
                """
            )
           .AddFile(
                "app/GreetingsOptions.cs",
                """
                namespace app;

                /// <summary>How the application greets.</summary>
                [Optionary.Optionary]
                public class GreetingsOptions
                {
                    /// <summary>Text placed before the name.</summary>
                    public string Message { get; set; } = "";

                    public int Count { get; set; }
                }
                """
            );

        await project.BuildProject("app");
        await project.Warnings("app").Should().BeEmpty();

        var output = await project.Run("app");

        await output.Should().Contain("Hello from configuration x3");
        var schema = project.ReadFile("app/obj/appsettings.schema.json");
        await schema.Should().Contain("Unknown key in section \\\"Greetings\\\"");
        await schema.Should().Contain("Text placed before the name.");
    }
}

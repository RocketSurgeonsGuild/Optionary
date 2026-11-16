using System.Diagnostics;
using System.Text;
using System.Text.Json;
using TUnit.Core.Interfaces;

namespace Optionary.Configuration.Json.Tests;

/// <summary>
///     Throwaway projects outside the repository, without Dovetail, that reference the packed Optionary.Configuration.Json package: an
///     application (<c>app</c>), a library (<c>lib</c>) and a test project (<c>tests</c>, a library with <c>IsTestProject</c>). They
///     restore into their own packages folder, so the package under test never reaches the machine's global package cache.
/// </summary>
public sealed class ConsumerProjects : IAsyncInitializer, IDisposable
{
    private static readonly string TargetFramework = $"net{Environment.Version.Major}.0";

    public string Directory { get; } = System.IO.Directory.CreateDirectory(
        Path.Combine(Path.GetTempPath(), "optionary-json-consumers-" + Guid.NewGuid().ToString("N"))
    ).FullName;

    public async Task InitializeAsync()
    {
        await Write(
            "NuGet.config",
            $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="optionary-json" value="{JsonPackage.Directory}" />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
              <packageSourceMapping>
                <clear />
                <packageSource key="optionary-json">
                  <package pattern="{JsonPackage.Id}" />
                </packageSource>
                <packageSource key="nuget.org">
                  <package pattern="*" />
                </packageSource>
              </packageSourceMapping>
            </configuration>
            """
        );
        await Write(
            "Directory.Build.props",
            $"""
            <Project>
              <PropertyGroup>
                <TargetFramework>{TargetFramework}</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <RestorePackagesPath>$(MSBuildThisFileDirectory)packages</RestorePackagesPath>
                <NuGetAudit>false</NuGetAudit>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="{JsonPackage.Id}" Version="{JsonPackage.Version}" />
              </ItemGroup>
            </Project>
            """
        );
        await Write("app/app.csproj", Project("<OutputType>Exe</OutputType>"));
        await Write(
            "app/Program.cs",
            """
            using Microsoft.Extensions.Configuration;

            var configuration = new ConfigurationBuilder().AddJsonStream(new MemoryStream("{ \"Greeting\": \"hello\" }"u8.ToArray())).Build();
            Console.WriteLine(configuration["Greeting"]);
            """
        );
        await Write("lib/lib.csproj", Project(""));
        await Write("tests/tests.csproj", Project("<IsTestProject>true</IsTestProject>"));
        await Write(
            "consumers.slnx",
            """
            <Solution>
              <Project Path="app/app.csproj" />
              <Project Path="lib/lib.csproj" />
              <Project Path="tests/tests.csproj" />
            </Solution>
            """
        );

        var (ExitCode, Output) = await DotNet("restore", "consumers.slnx");
        if (ExitCode != 0) throw new InvalidOperationException($"The consumer projects didn't restore:\n{Output}");

        static string Project(string properties) =>
            $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>{properties}</PropertyGroup>
            </Project>
            """;
    }

    /// <summary>
    ///     Runs <paramref name="targets" /> (or only evaluates, when there are none) on <paramref name="project" /> and returns the
    ///     <paramref name="itemTypes" /> as they are afterwards.
    /// </summary>
    public async Task<MSBuildResult> MSBuild(string project, string[] targets, params string[] itemTypes)
    {
        List<string> arguments = ["msbuild", $"{project}/{project}.csproj", "-nologo"];
        if (targets is { Length: > 0 }) arguments.Add($"-t:{string.Join(';', targets)}");
        arguments.AddRange(itemTypes.Select(z => $"-getItem:{z}"));
        // -getItem prints JSON only for more than one value, so always ask for a property too.
        arguments.Add("-getProperty:OutputType");

        var (exitCode, output) = await DotNet([.. arguments]);
        if (exitCode != 0) return new(exitCode, output, new Dictionary<string, IReadOnlyList<MSBuildItem>>());

        using var json = JsonDocument.Parse(output[output.IndexOf('{', StringComparison.Ordinal)..]);
        var items = new Dictionary<string, IReadOnlyList<MSBuildItem>>(StringComparer.OrdinalIgnoreCase);
        foreach (var itemType in itemTypes)
        {
            items[itemType] = json.RootElement.TryGetProperty("Items", out var all) && all.TryGetProperty(itemType, out var ofType)
                ? ofType.EnumerateArray()
                        .Select(
                             item => new MSBuildItem(
                                 item.EnumerateObject()
                                     .Where(z => z.Value.ValueKind == JsonValueKind.String)
                                     .ToDictionary(z => z.Name, z => z.Value.GetString() ?? "", StringComparer.OrdinalIgnoreCase)
                             )
                         )
                        .ToArray()
                : [];
        }

        return new(exitCode, output, items);
    }

    public void Dispose()
    {
        try
        {
            System.IO.Directory.Delete(Directory, true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private Task Write(string relativePath, string content)
    {
        var path = Path.Combine(Directory, relativePath);
        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return File.WriteAllTextAsync(path, content);
    }

    private async Task<(int ExitCode, string Output)> DotNet(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host ? host : "dotnet")
        {
            WorkingDirectory = Directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
        startInfo.ArgumentList.Add("-nodeReuse:false");
        // The test host can inherit MSBuild's environment from the build that started it; the consumers get a clean one.
        foreach (var name in startInfo.Environment.Keys.Where(z => z.StartsWith("MSBuild", StringComparison.OrdinalIgnoreCase)).ToArray())
        {
            startInfo.Environment.Remove(name);
        }

        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.Environment["DOTNET_NOLOGO"] = "1";

        using var process = Process.Start(startInfo)!;
        var output = new StringBuilder();
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        output.Append(await standardOutput).Append(await standardError);
        return (process.ExitCode, output.ToString());
    }
}

public sealed record MSBuildResult(int ExitCode, string Output, IReadOnlyDictionary<string, IReadOnlyList<MSBuildItem>> Items);

public sealed record MSBuildItem(IReadOnlyDictionary<string, string> Metadata)
{
    public string Identity => Metadata["Identity"];

    public string this[string name] => Metadata.TryGetValue(name, out var value) ? value : "";
}

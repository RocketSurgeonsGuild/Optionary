# Optionary

Strongly-typed, AOT-safe configuration options for .NET: a reflection-free options binder and per-application JSON Schema for editor completion, generated from your options types.

It ships as one package, `Optionary`: the `[Optionary]` attribute, the source generator, the binder and the schema build.
Optionary reads whatever `IConfiguration` holds and adds no configuration providers of its own; use Microsoft's JSON
provider or any other `IConfigurationSource` for the files.

[![github-release-badge]][github-release]
[![github-license-badge]][github-license]
[![github-build-badge]][github-build]
[![nuget-Optionary-badge]][nuget-Optionary]

Documentation: <https://rocketsurgeonsguild.github.io/Optionary/>

## Getting started

Add the package:

```sh
dotnet add package Optionary
```

Put `[Optionary]` on the class that holds a section's values. The section name is the type name without its `Options`
suffix:

```csharp
using System.ComponentModel.DataAnnotations;
using Optionary;

namespace MyApp;

/// <summary>Options for the greetings this application prints.</summary>
[Optionary] // section "Greetings"
public sealed class GreetingsOptions
{
    /// <summary>The text that comes before the name.</summary>
    public string Message { get; set; } = "Hello";

    /// <summary>How many times the greeting is said.</summary>
    [Range(1, 5)]
    public int Repeat { get; set; } = 1;
}
```

The generator writes an internal `AddOptionaryConfigurationOptions()` extension method into the project's
`RootNamespace`. Call it once, then read the options through `IOptions<GreetingsOptions>` as usual:

```csharp
using MyApp;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddOptionaryConfigurationOptions();
```

Building an application writes `obj/appsettings.schema.json`, merged from its own options types and those of every library
it references, which editors use for completion and validation of `appsettings.json`. See
[Getting started](https://rocketsurgeonsguild.github.io/Optionary/getting-started/) for the editor setup, and the
[samples](https://rocketsurgeonsguild.github.io/Optionary/samples/) for a library, a console app and a web app.

## Packages

| Package                      | NuGet                                       | What it does                                                                                     |
| ---------------------------- | ------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| [Optionary][nuget-Optionary] | [![nuget-Optionary-badge]][nuget-Optionary] | The `[Optionary]` attributes, the source-generated binder and the MSBuild targets for the schema |

## Building

The toolchain comes from [mise](https://mise.jdx.dev): `mise install`, then `mise run build` (or `dotnet build Optionary.slnx`).
`mise run docs` serves the documentation site from `docs/`.

## Origins

Optionary started as the configuration schema and binding feature of
[Dovetail](https://github.com/RocketSurgeonsGuild/Dovetail), and was moved into its own library so it can be used without
Dovetail. The code was imported from Dovetail at commit
[`3f512a33b0`](https://github.com/RocketSurgeonsGuild/Dovetail/commit/3f512a33b0) as a snapshot, without its history, and
then renamed; Dovetail's history up to that commit is where to look for how it came to be. The
[migration plan](https://github.com/RocketSurgeonsGuild/Optionary/blob/main/plans/migration-from-dovetail.md) tracks what
has moved so far.

[github-release]: https://github.com/RocketSurgeonsGuild/Optionary/releases
[github-release-badge]: https://img.shields.io/github/v/release/RocketSurgeonsGuild/Optionary?include_prereleases&logo=github&style=flat
[github-license]: https://github.com/RocketSurgeonsGuild/Optionary/blob/main/LICENSE
[github-license-badge]: https://img.shields.io/github/license/RocketSurgeonsGuild/Optionary.svg?style=flat
[github-build]: https://github.com/RocketSurgeonsGuild/Optionary/actions/workflows/build.yml
[github-build-badge]: https://github.com/RocketSurgeonsGuild/Optionary/actions/workflows/build.yml/badge.svg
[nuget-Optionary]: https://www.nuget.org/packages/Optionary/
[nuget-Optionary-badge]: https://img.shields.io/nuget/v/Optionary.svg?logo=nuget&style=flat

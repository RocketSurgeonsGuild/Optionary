---
title: Samples
description: A library, a console application and an ASP.NET Core application that use Optionary without any host framework.
tags: [getting-started, configuration, schema]
---

The repository's [`samples/`](https://github.com/RocketSurgeonsGuild/Optionary/tree/main/samples) folder has three projects
that use Optionary on its own, with Microsoft's configuration providers and no host framework.

| Project                                                                                                                     | Shows                                                                                                                                              |
| --------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| [`Optionary.Samples.Library`](https://github.com/RocketSurgeonsGuild/Optionary/tree/main/samples/Optionary.Samples.Library) | A library's declarations, its validator, and the public `AddSampleLibrary()` that calls the generated registration method.                         |
| [`Optionary.Samples.Console`](https://github.com/RocketSurgeonsGuild/Optionary/tree/main/samples/Optionary.Samples.Console) | A console host that binds the library's options from `appsettings.json` through `IOptions<T>` and `IOptionsMonitor<T>`, and prints them on reload. |
| [`Optionary.Samples.Web`](https://github.com/RocketSurgeonsGuild/Optionary/tree/main/samples/Optionary.Samples.Web)         | An ASP.NET Core host with a declaration of its own that fails the application at startup when it is invalid (`ValidateOnStart`).                   |

## Run them

```sh
mise run sample:console           # prints the bound options
mise run sample:console --watch   # and prints them again when an appsettings*.json file changes
mise run sample:web               # serves the bound options as JSON on http://localhost:5180
```

Without mise, use `dotnet run --project samples/Optionary.Samples.Console` (add `-- --watch` to watch) and
`dotnet run --project samples/Optionary.Samples.Web`.

The console sample shows layering with Microsoft's providers: run it with `DOTNET_ENVIRONMENT=Development` and
`appsettings.Development.json` overrides the greeting, or set `Greetings__Message=Hi` to override it from the environment.

## The library

[`Optionary.Samples.Library`](https://github.com/RocketSurgeonsGuild/Optionary/tree/main/samples/Optionary.Samples.Library)
declares four sections:

- `GreetingsOptions`: the class form, `[Optionary]`, with `[Description]` and `[Range]` that reach the schema. Section
  `Greetings`.
- `FarewellOptions`: a record. The generated binder calls its constructor. Section `Farewell`.
- `RetryPolicyOptions`: `KeyStyle = ConfigurationKeyStyle.Underscore`, so the section is `retry_policy` and its keys are
  `max_attempts` and `delay` (see [key styles](../guides/key-styles/)).
- `ClockSettings`: no attribute of its own, declared with `[assembly: Optionary<ClockSettings>("Samples:Clock")]`, the form
  for types you can't annotate (see [declarations](../guides/declarations/#on-the-assembly-assemblyoptionaryt)).

`GreetingsOptionsValidator` is an `[OptionsValidator]`, which the registration method registers with the library's options.
`AddSampleLibrary()` calls the internal, generated `AddOptionaryConfigurationOptions()`, as
[registering from a library](../guides/registering-from-a-library/) describes.

Its build writes the library schema, `ConfigurationSchema.json`, under `obj/`. The samples aren't packable, so CI never
publishes them; to see the file at the package root, run
`dotnet pack samples/Optionary.Samples.Library -p:SamplePack=true`.

## The hosts

Both applications are hosts, so each build writes `obj/appsettings.schema.json`, merged from the host's own declarations and
the library's, with SchemaStore's `appsettings.json` schema for `Logging` and the other framework sections. The console
sample adds a `Console` section of its own, and the web sample a `Site` section with `ValidateOnStart = true` and its own
validator. The web sample also calls `AddOptions<GreetingsOptions>().ValidateOnStart()`, to validate a library's options at
startup when the library didn't ask for it. Start it with an empty title to see it fail:

```sh
Site__Title= dotnet run --project samples/Optionary.Samples.Web
```

Each sample's `appsettings*.json` starts with `"$schema": "./obj/appsettings.schema.json"`, pointing at its host's merged
schema, so any editor that honours `$schema` picks it up without settings. Build the samples once and the files get
completion, hover documentation and checks for unknown keys and out-of-range values. The key is ordinary configuration to
the binder (a root `$schema` value no section reads), and the merged schema leaves the root open, so it validates. To map
files without editing them, use `OptionaryScaffoldEditorSettings` instead (see
[schema and editors](../guides/schema-and-editors/#visual-studio-code)).

## How they consume Optionary

The samples use Optionary from this repository's source, wired as the package wires itself into a consumer:

- [`samples/Directory.Build.props`](https://github.com/RocketSurgeonsGuild/Optionary/blob/main/samples/Directory.Build.props)
  imports the package's `build/Optionary.props`;
- [`samples/Directory.Build.targets`](https://github.com/RocketSurgeonsGuild/Optionary/blob/main/samples/Directory.Build.targets)
  references the runtime project and the generator as an analyzer, and imports the package's `build/Optionary.targets` with
  the MSBuild tasks built in the repository.

In your own projects, `dotnet add package Optionary` does all of this (see [getting started](../getting-started/)).

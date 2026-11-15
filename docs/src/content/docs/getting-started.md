---
title: Getting started
description: Add Optionary, declare an options type, register it, and get completion for its section in appsettings files.
tags: [getting-started, configuration]
---

## Requirements

- A project targeting `net10.0` or `net11.0`.
- C# 12 or later. With an older `LangVersion` the generator binds nothing and reports
  [`OPT0001`](../reference/diagnostics/#opt0001).
- A compiler that can load Roslyn 5.0 or later analyzers (the .NET 10 SDK or later). With an older one, the generator isn't
  loaded and the build fails with [`OPT1001`](../reference/diagnostics/#opt1001).

## Add the package

```sh
dotnet add package Optionary
```

The package carries the runtime library, the source generator and the MSBuild targets that write the schema. Its build
assets flow transitively, so a project that references a library built with Optionary merges that library's fragments
into its own application schema.

## Declare an options type

Put `[Optionary]` on the class (or record) that holds the section's values:

```csharp
using System.ComponentModel.DataAnnotations;
using Optionary;

namespace MyApp;

/// <summary>Options for the greetings this application prints.</summary>
[Optionary] // section "Greetings": the type name without its "Options" suffix
public sealed class GreetingsOptions
{
    /// <summary>The text that comes before the name.</summary>
    public string Message { get; set; } = "Hello";

    /// <summary>How many times the greeting is said.</summary>
    [Range(1, 5)]
    public int Repeat { get; set; } = 1;
}
```

The [declarations](../guides/declarations/) guide covers explicit section paths, the assembly form for types you don't own,
and the other attribute properties.

## Register it

The generator writes a **registration method** into the assembly: an internal extension method,
`AddOptionaryConfigurationOptions()`, in a class named `OptionaryConfigurationOptions` in the project's `RootNamespace`. Call it
once on the service collection:

```csharp
using MyApp; // the project's RootNamespace, where the registration method lives

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddOptionaryConfigurationOptions();
```

Then read the options the usual way, through `IOptions<GreetingsOptions>`, `IOptionsSnapshot<T>` or `IOptionsMonitor<T>`:

```csharp
public sealed class Greeter(IOptions<GreetingsOptions> options)
{
    public string Greet(string name) => $"{options.Value.Message}, {name}";
}
```

The binder is generated code, so nothing here uses reflection. A library calls its own registration method from a public
extension method instead; see [registering from a library](../guides/registering-from-a-library/).

## Write the configuration

```json title="appsettings.json"
{
    "Greetings": {
        "Message": "Howdy",
        "Repeat": 2
    }
}
```

## Point your editor at the schema

Build the project once. An application or test project is a **host**, and its build writes the **application schema** to
`obj/appsettings.schema.json`, merged from its own fragments and those of every assembly it references.

- **Visual Studio** picks it up for `appsettings*.json` with no setup.
- **VS Code** needs a mapping in `.vscode/settings.json`, which Optionary can write for you: set
  `<OptionaryScaffoldEditorSettings>true</OptionaryScaffoldEditorSettings>`.
- **Rider** needs a JSON Schema mapping.

[Schema and editors](../guides/schema-and-editors/) has the details for each.

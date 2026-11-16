---
title: TOML with Dovetail
description: Optionary.Configuration.Toml, the Dovetail joint templates that load an application's TOML configuration files.
tags: [toml, configuration]
---

`Optionary.Configuration.Toml` ships three Dovetail joint templates that load an application's TOML files. The TOML
provider comes from [Tomlyn.Extensions.Configuration](https://www.nuget.org/packages/Tomlyn.Extensions.Configuration),
which is the package's only dependency. The package doesn't depend on `Optionary`, and `Optionary` doesn't need it.

```sh
dotnet add package Optionary.Configuration.Toml
```

## Without Dovetail

Without Dovetail the package only brings in Tomlyn's provider. Use it directly:

```csharp
using Tomlyn.Extensions.Configuration;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration
    .AddTomlFile("appsettings.toml", optional: true, reloadOnChange: true)
    .AddTomlFile($"appsettings.{builder.Environment.EnvironmentName}.toml", optional: true, reloadOnChange: true);
```

```toml title="appsettings.toml"
[Greetings]
Message = "Howdy"
Repeat = 2
Recipients = ["Ada", "Grace"]
```

The package's assembly has no API of its own. It ships so that Dovetail can tell an application references the package
(see below).

## Schemas for TOML files

The [application schema](../schema-and-editors/) describes TOML files as well as JSON ones: map `appsettings*.toml` to it
in your editor, or let `OptionaryScaffoldEditorSettings` write the VS Code mapping.

## With Dovetail

The package's `build/Optionary.Configuration.Toml.targets` declares three Dovetail joint templates, so a Dovetail
application gets its TOML files loaded without any code. Only Dovetail reads `DovetailTemplate` items, and only in a project
that runs Dovetail's generator and references this package; without Dovetail they do nothing, and nothing extra is compiled.

| `DovetailTemplate`  | Generated joint                                 | What it adds                                                                                                                                                    |
| ------------------- | ----------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `TomlJoint`         | `TomlJoint` (`IConfigurationJoint`)             | `appsettings.toml`, `appsettings.{Environment}.toml`, the same named after the application, and the `.local` overrides, from the configuration's file provider. |
| `TomlBrowserJoint`  | `TomlBrowserJoint` (`IConfigurationAsyncJoint`) | The same files in the browser (WebAssembly), downloaded from the application's base address.                                                                    |
| `TomlEmbeddedJoint` | `TomlEmbeddedJoint` (`IConfigurationJoint`)     | The TOML files of hosts that embed their configuration in the assembly (MAUI, Uno), read with `AddTomlStream`.                                                  |

- Each item has `Package="Optionary.Configuration.Toml"` and `OutputPrefix="OptionaryToml"`. Dovetail emits a template
  only into a project whose resolved references include the `Optionary.Configuration.Toml` assembly. The joints are
  generated as internal classes in the namespace `Optionary.Configuration.Toml`, and each adds its files once even when a
  test project imports the application's copy as well as its own.
- The items are declared only in applications (`OutputType` `Exe` or `WinExe`, or an Android application) and test
  projects (`IsTestProject`). A joint compiled into a library would be exported to every application that references it, so
  libraries never get one, and each application gets exactly one of each.
- The targets also add a `BlazorConfigurationFile` assembly metadata entry listing `wwwroot/appsettings*.toml`, which the
  browser joint uses to know which files to download.

---
title: JSON joints
description: Optionary.Configuration.Json, the Dovetail joint templates that load an application's JSON configuration files.
tags: [json, configuration, dovetail]
---

`Optionary.Configuration.Json` holds Dovetail's JSON configuration joints as templates. It has no API of its own: JSON
files are read by `Microsoft.Extensions.Configuration.Json`, its only dependency. It doesn't depend on `Optionary`, and
`Optionary` doesn't need it.

```sh
dotnet add package Optionary.Configuration.Json
```

Without Dovetail the package does nothing beyond bringing in `Microsoft.Extensions.Configuration.Json`. Add JSON files with
its own methods (`AddJsonFile`, `AddJsonStream`). The [application schema](../schema-and-editors/) describes
`appsettings*.json` either way.

## With Dovetail

The package's `build/Optionary.Configuration.Json.targets` declares three Dovetail joint templates, so a Dovetail
application gets its JSON files loaded without any code. Only Dovetail reads `DovetailTemplate` items, and only in a
project that runs Dovetail's generator and references this package; without Dovetail they do nothing, and nothing extra is
compiled.

| `DovetailTemplate`  | Generated joint                                 | What it adds                                                                                                                                                    |
| ------------------- | ----------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `JsonJoint`         | `JsonJoint` (`IConfigurationJoint`)             | `appsettings.json`, `appsettings.{Environment}.json`, the same named after the application, and the `.local` overrides, from the configuration's file provider. |
| `JsonBrowserJoint`  | `JsonBrowserJoint` (`IConfigurationAsyncJoint`) | The same files in the browser (WebAssembly), downloaded from the application's base address.                                                                    |
| `JsonEmbeddedJoint` | `JsonEmbeddedJoint` (`IConfigurationJoint`)     | The JSON files of hosts that embed their configuration in the assembly (MAUI, Uno), read with `AddJsonStream`.                                                  |

- Each item has `Package="Optionary.Configuration.Json"` and `OutputPrefix="OptionaryJson"`. The joints are generated as
  internal classes in the namespace `Optionary.Configuration.Json`, and each adds its files once even when a test project
  imports the application's copy as well as its own.
- The items are declared only in applications (`OutputType` `Exe` or `WinExe`, or an Android application) and test
  projects (`IsTestProject`). A joint compiled into a library would be exported to every application that references it, so
  libraries never get one, and each application gets exactly one of each.
- The package ships an `Optionary.Configuration.Json` assembly with no types in it. Dovetail emits a template only into a
  project whose resolved references include the assembly named after the template's `Package`.
- The targets also add a `BlazorConfigurationFile` assembly metadata entry listing `wwwroot/appsettings*.json`, which the
  browser joint uses to know which files to download.

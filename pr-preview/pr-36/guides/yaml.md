# YAML provider

`Optionary.Configuration.Yaml` is a YAML configuration provider for `Microsoft.Extensions.Configuration`, built on
YamlDotNet. It is its own package and doesn't depend on `Optionary`: you can use it without declarations or schemas, and
`Optionary` doesn't need it. It is trimming and Native AOT compatible.

```sh
dotnet add package Optionary.Configuration.Yaml
```

## Adding YAML files

```csharp
using Optionary.Configuration.Yaml;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration
    .AddYamlFile("appsettings.yaml", optional: true, reloadOnChange: true)
    .AddYamlFile($"appsettings.{builder.Environment.EnvironmentName}.yaml", optional: true, reloadOnChange: true);
```

```yaml title="appsettings.yaml"
Greetings:
    Message: Howdy
    Repeat: 2
    Recipients:
        - Ada
        - Grace
```

| Method                                                                 | Adds                                                            |
| ---------------------------------------------------------------------- | --------------------------------------------------------------- |
| `AddYamlFile(path)`                                                    | A required file, relative to the builder's base path.           |
| `AddYamlFile(path, optional)`                                          | A file that may be missing.                                     |
| `AddYamlFile(path, optional, reloadOnChange)`                          | The same, reloaded when the file changes.                       |
| `AddYamlFile(IFileProvider? provider, path, optional, reloadOnChange)` | A file read through the given file provider.                    |
| `AddYamlFile(Action<YamlConfigurationSource> configureSource)`         | A source you configure yourself.                                |
| `AddYamlStream(Stream stream)`                                         | YAML read once from a stream, for example an embedded resource. |

## How YAML maps to configuration

- Mappings become nested keys joined with `:`, so `Greetings: { Message: Howdy }` is `Greetings:Message`.
- Sequences become indexed keys: `Greetings:Recipients:0`, `Greetings:Recipients:1`.
- A plain (unquoted) `~`, `null`, `Null` or `NULL` is a null value. Quote it to keep the text.
- Keys ignore case, as everywhere in configuration, so a key that appears twice in any casing is a `FormatException`.
- Only the first document of a multi-document file is read, and its root must be a mapping. YAML that doesn't parse is a
  `FormatException` with YamlDotNet's message.

## Schemas for YAML files

The [application schema](../schema-and-editors/) describes YAML files as well as JSON ones: map `appsettings*.yaml` to it in
your editor, or let `OptionaryScaffoldEditorSettings` write the VS Code mapping.

## With Dovetail

The package's `build/Optionary.Configuration.Yaml.targets` declares three Dovetail joint templates, so a Dovetail application
gets its YAML files loaded without any code. Only Dovetail reads `DovetailTemplate` items, and only in a project that runs
Dovetail's generator and references this package; without Dovetail they do nothing, and nothing extra is compiled.

| `DovetailTemplate`  | Generated joint                                 | What it adds                                                                                                                                                                         |
| ------------------- | ----------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `YamlJoint`         | `YamlJoint` (`IConfigurationJoint`)             | `appsettings.yaml`, `appsettings.{Environment}.yaml`, the same named after the application, and the `.local` overrides, each also as `.yml`, from the configuration's file provider. |
| `YamlBrowserJoint`  | `YamlBrowserJoint` (`IConfigurationAsyncJoint`) | The same files in the browser (WebAssembly), downloaded from the application's base address.                                                                                         |
| `YamlEmbeddedJoint` | `YamlEmbeddedJoint` (`IConfigurationJoint`)     | The YAML files of hosts that embed their configuration in the assembly (MAUI, Uno), read with `AddYamlStream`.                                                                       |

- Each item has `Package="Optionary.Configuration.Yaml"` and `OutputPrefix="OptionaryYaml"`. The joints are generated as
  internal classes in the namespace `Optionary.Configuration.Yaml`, and each adds its files once even when a test project
  imports the application's copy as well as its own.
- The items are declared only in applications (`OutputType` `Exe` or `WinExe`, or an Android application) and test
  projects (`IsTestProject`). A joint compiled into a library would be exported to every application that references it, so
  libraries never get one, and each application gets exactly one of each.
- The targets also add a `BlazorConfigurationFile` assembly metadata entry listing `wwwroot/appsettings*.yml` and
  `wwwroot/appsettings*.yaml`, which the browser joint uses to know which files to download.
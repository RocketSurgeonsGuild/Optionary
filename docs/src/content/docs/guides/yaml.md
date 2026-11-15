---
title: YAML provider
description: Optionary.Configuration.Yaml, a YAML configuration provider for Microsoft.Extensions.Configuration.
tags: [yaml, configuration]
---

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

The package also declares Dovetail joint templates (`DovetailTemplate` items), only in applications and test projects, so
that a Dovetail application gets the YAML provider wired up for it. Without Dovetail nothing reads those items and they do
nothing.

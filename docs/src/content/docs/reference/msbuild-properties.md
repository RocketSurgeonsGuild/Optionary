---
title: MSBuild properties
description: Every MSBuild property, item and item metadata Optionary reads, with defaults.
tags: [msbuild, schema]
---

These properties and items are Optionary's public MSBuild API. They change only in a major version. Set them in the project file or
in a `Directory.Build.props`:

```xml
<PropertyGroup>
  <!-- Permissive: unknown keys under Optionary sections aren't flagged -->
  <OptionarySchemaStrict>false</OptionarySchemaStrict>
</PropertyGroup>
```

## Schema files

| Property                            | Default  | Effect                                                                                                                                                                              |
| ----------------------------------- | -------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `OptionarySchema`                   | on       | `false` writes no schema files, no Visual Studio segments and no editor settings, and removes the files an earlier build wrote.                                                     |
| `OptionarySchemaHost`               | detected | `true` or `false` overrides which projects are hosts. Left empty, test projects (`IsTestProject`) and applications (`OutputType` `Exe` or `WinExe`, or an Android application) are. |
| `OptionarySchemaStrict`             | `true`   | `false` leaves Optionary's sections open (no `propertyNames` check) in the application schema.                                                                                      |
| `OptionarySchemaIncludeSchemaStore` | `true`   | `false` leaves SchemaStore's `appsettings.json` schema out of the application schema.                                                                                               |
| `OptionarySchemaCopyToOutput`       | `true`   | `false` keeps a host's application schema in `obj/`: it isn't copied to the output directory or published. For hosts that embed their configuration (MAUI, Uno).                    |
| `OptionarySchemaRequired`           | `false`  | `true` lists required members in each fragment's `required`. The generator reads this one, so it changes the fragments in every project that sets it.                               |
| `OptionaryReferenceDocumentation`   | on       | `false` doesn't pass referenced packages' XML documentation files to the generator, so types declared with `[assembly: Optionary<T>]` get no descriptions from them.                |

## Editors

| Property                           | Default  | Effect                                                                                                                                                                |
| ---------------------------------- | -------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `OptionarySchemaVisualStudio`      | on       | `false` adds no `JsonSchemaSegment` items for Visual Studio; the schema files are still written.                                                                      |
| `OptionaryScaffoldEditorSettings`  | off      | `true` writes Optionary's mappings into `.vscode/settings.json` when a host builds. See [VS Code](../../guides/schema-and-editors/#let-optionary-write-the-settings). |
| `OptionaryEditorSettingsDirectory` | git root | The folder that holds `.vscode`. Relative paths are relative to the project. By default, the nearest ancestor with a `.git` entry, or the project folder.             |

## Generator

| Property                       | Default | Effect                                                                                                                                                              |
| ------------------------------ | ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `RootNamespace`                | SDK     | The namespace of the generated [registration method](../../guides/registering-from-a-library/), used verbatim. Empty or whitespace puts it in the global namespace. |
| `OptionaryZeroAllocValidation` | on      | `false` leaves ZeroAlloc.Validation's registrations to the project: the registration method doesn't call `ValidateWithZeroAlloc()`.                                 |
| `LangVersion`                  | SDK     | C# 12 or later is required. Below that the registration method is empty and [`OPT0001`](../diagnostics/#opt0001) is reported.                                       |

## Items

| Item                      | Metadata                        | Effect                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| ------------------------- | ------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `OptionarySchemaFragment` | `Schema` or `File`, exactly one | `Include` is a section path. `Schema` is the schema of the section's value as inline JSON, `File` a JSON file holding it (relative to the project), in the [fragment format](../../guides/schema-and-editors/#fragments). It becomes the project's fragment for that section, like one the generator writes. A package's `buildTransitive` targets can add it. See [contributing a fragment](../../guides/schema-and-editors/#contributing-a-fragment-from-a-package-or-another-generator). |

## Item metadata

| Item              | Metadata                                 | Effect                                                                                                                                                                                                       |
| ----------------- | ---------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `AdditionalFiles` | `OptionaryReferenceDocumentation="true"` | Marks an XML documentation file the generator reads descriptions from, for types declared with `[assembly: Optionary<T>]`. Optionary adds these itself for package references; add your own for other files. |

## Files

| File                                                             | Written by                                    | What it is                                                                                                                                                              |
| ---------------------------------------------------------------- | --------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `$(BaseIntermediateOutputPath)appsettings.schema.json`           | hosts                                         | The application schema, at a stable path (normally `obj/appsettings.schema.json`). Copied to `$(OutDir)` and published unless `OptionarySchemaCopyToOutput` is `false`. |
| `$(IntermediateOutputPath)appsettings.schema.json`               | multi-target hosts                            | The application schema for each target framework after the first.                                                                                                       |
| `$(IntermediateOutputPath)ConfigurationSchema.json`              | libraries                                     | The library schema, packed at the package root.                                                                                                                         |
| `$(IntermediateOutputPath)<project>.OptionarySchemaFragments.cs` | projects with `OptionarySchemaFragment` items | The fragments' attributes, when `GenerateAssemblyInfo` or `GenerateAssemblyMetadataAttributes` is `false`; otherwise they are in the SDK's `AssemblyInfo.cs`.           |

## For host frameworks

A framework that builds on Optionary can set `OptionarySchemaHost`, `OptionarySchemaCopyToOutput` and
`OptionaryZeroAllocValidation` from its own targets. Optionary reads them when the schema is computed, not at evaluation, so
a value set anywhere during evaluation counts, and setting them has no effect in a project that doesn't reference Optionary.
Dovetail does this from its application detection and its embedded-configuration and ZeroAlloc switches.

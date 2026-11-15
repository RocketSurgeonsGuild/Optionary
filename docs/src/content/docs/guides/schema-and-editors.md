---
title: Schema and editors
description: Fragments, the library schema, the merged application schema, and how Visual Studio, VS Code and Rider use it.
tags: [schema, msbuild, configuration]
---

Every application built with Optionary gets one JSON Schema for its configuration, merged from the declarations of every
assembly it references. Editors use it to complete section and key names in `appsettings.json`, YAML and TOML files, to show
the documentation you wrote on the options types, and to flag unknown keys and out-of-range values.

## Fragments

The generator embeds one **fragment** per section in the assembly:

```csharp
[assembly: AssemblyMetadata("Optionary.Schema.v1:<section path>", "<JSON Schema of the section's value>")]
```

Each fragment is a Draft-07 schema of the section's value, with every object type described once under `definitions`.
Every key also has a case-insensitive `patternProperties` twin with the same schema, so `retries: 99` is checked even when
written as `Retries`.

Hand-written fragments in the same format are supported: a library whose section isn't described by an options type can
add the attribute itself, and it is merged like any other. Only the `Optionary.Schema.v1:` prefix is read.

### Annotations that reach the schema

| Source                                      | Schema                                                    |
| ------------------------------------------- | --------------------------------------------------------- |
| `[Description]`, else the XML `<summary>`   | `description`. For a record parameter, its `<param>`.     |
| `[Range]`, length attributes                | `minimum`/`maximum`, `minLength`/`maxLength`, item counts |
| `[RegularExpression]`                       | `pattern`, when the expression can be anchored            |
| `[Url]`, `[EmailAddress]`                   | `format`                                                  |
| `[Obsolete]`                                | `deprecated` and a deprecation message                    |
| A constant initializer or parameter default | `default`, except an empty string                         |

- Ranges and defaults are also written into the description, since editors don't show them.
- The XML summary is read from source whether or not `GenerateDocumentationFile` is on. `<inheritdoc />` and `<include>`
  produce no description.
- Annotations JSON Schema can't express are dropped with [`OPT0008`](../../reference/diagnostics/#opt0008).
- Fragments are permissive: nothing is `required` unless `OptionarySchemaRequired` is `true`, which marks `required`
  members, `[Required]` properties and value-type constructor parameters without defaults.

## The library schema

Every project that isn't a host (a library) writes its own fragments to `$(IntermediateOutputPath)ConfigurationSchema.json`
and packs it at the NuGet package root. This **library schema** is permissive and rooted at the configuration root, the
layout Aspire's packages use, so Visual Studio and other tools that understand it can read it. A library that targets
several frameworks writes one per framework and packs the first one's. A library with no sections writes nothing.

## The application schema

A **host** is a test project (`IsTestProject`) or an application (`OutputType` `Exe` or `WinExe`, or an Android
application). Set `OptionarySchemaHost` to `true` or `false` to override the detection; a host framework can set it from its
own application detection.

A host **merges** its own fragments and those of every referenced assembly into
`$(BaseIntermediateOutputPath)appsettings.schema.json`, normally `obj/appsettings.schema.json`.

- **Stable path.** Editors can point at it and the setting can be checked in. A host that targets several frameworks
  writes it from its first target framework, and `$(IntermediateOutputPath)appsettings.schema.json` (for example
  `obj/Debug/net10.0/`) from the others.
- **Copied and published.** It is copied to `$(OutDir)` next to `appsettings.json` and published as
  `appsettings.schema.json`. A project that references the host (a test project referencing an application) doesn't get a
  second copy. Set `OptionarySchemaCopyToOutput` to `false` to keep it in `obj/` only, for hosts that embed their
  configuration (MAUI, Uno).
- **No collisions.** A hand-made `appsettings.schema.json` (Web SDK content, or one from a reference) is removed from the
  publish items so it doesn't collide with the generated one (NETSDK1152).
- **Strict.** Each Optionary section, and the object types it uses, accepts only its own keys. The check is a
  case-insensitive `propertyNames` pattern rather than `additionalProperties: false`, so `message` is accepted and `Mesage`
  is flagged, with an error that lists the known keys. The root, parents shared between sections, dictionaries and sections
  declared by more than one assembly stay open. `OptionarySchemaStrict=false` turns the check off.
- **Includes SchemaStore.** SchemaStore's `appsettings.json` schema is embedded, not referenced, so `Logging`, `Kestrel` and
  `ConnectionStrings` complete too, online or offline. The copy ships with Optionary (Apache-2.0).
  `OptionarySchemaIncludeSchemaStore=false` leaves it out.
- **Deterministic.** Section paths compare ignoring case. When fragments spell a section or key differently, the
  ordinal-smallest spelling is shown. A section several assemblies declare is merged into one object. The same fragments
  always give a byte-identical file, whatever order the references come in.

> [!NOTE]
> Editors flag a quoted number such as `"Timeout": "30"` as the wrong type, although the binder accepts it. The schema
> stays strict about types so that ranges are always checked.

The build target runs after `CoreCompile`, skips design-time builds and writes only when its inputs change. Merging runs
in a compiled MSBuild task that reads fragments with `System.Reflection.Metadata`: no assembly is loaded, and nothing is
embedded in your application. All the properties are in the [MSBuild reference](../../reference/msbuild-properties/).

## Visual Studio

Visual Studio's JSON editor reads schemas from `JsonSchemaSegment` items. Optionary adds them with the file pattern
`appsettings\..*json`, the one Aspire's packages use:

- a segment for the host's `obj/appsettings.schema.json`;
- a segment for each referenced package that ships a `ConfigurationSchema.json` at its root, as libraries built with
  Optionary do. A package that declares its own segment for the file (Aspire's client packages do) is skipped, so
  completions aren't doubled. Project references need none: their fragments are in the host's file.

Nothing needs configuring. The items are added after `ResolveAssemblyReferences`, so they exist in the design-time builds
Visual Studio runs when it loads the project. `OptionarySchemaVisualStudio=false` turns the segments off and keeps the
schema files. If completion doesn't appear after the first build, close and reopen the file.

> [!NOTE]
> Segments only affect Visual Studio's JSON editor; YAML and TOML files get nothing from them. Optionary's tests check the
> items MSBuild produces, including in a design-time build, but whether Visual Studio reads items a target adds, and how it
> merges two segments that describe the same section, has not been verified in Visual Studio itself.

## Visual Studio Code

VS Code needs one mapping per file type: its JSON support reads `json.schemas`, and the YAML (Red Hat) and Even Better
TOML extensions read `yaml.schemas` and `evenBetterToml.schema.associations`. Put them in `.vscode/settings.json`:

```jsonc
{
    "json.schemas": [
        {
            "fileMatch": ["/MyApp/appsettings*.json"],
            "url": "./MyApp/obj/appsettings.schema.json",
        },
    ],
    "yaml.schemas": {
        "./MyApp/obj/appsettings.schema.json": ["MyApp/appsettings*.yaml", "MyApp/appsettings*.yml"],
    },
    "evenBetterToml.schema.associations": {
        "^.+/MyApp/appsettings.*\\.toml$": "./MyApp/obj/appsettings.schema.json",
    },
}
```

- **JSON** reports unknown keys, completes the canonical names and shows descriptions on hover.
- **YAML** does the same. Its `fileMatch` globs don't match files under a dot-folder.
- **TOML** needs the explicit association: Even Better TOML can't use the SchemaStore catalog, and it reports an unknown
  key on the table header rather than on the key. It ignores `deprecated`.

### Let Optionary write the settings

Set `OptionaryScaffoldEditorSettings` in the application's project file:

```xml
<PropertyGroup>
  <OptionaryScaffoldEditorSettings>true</OptionaryScaffoldEditorSettings>
</PropertyGroup>
```

On build, Optionary creates or updates **only its own** entries in `.vscode/settings.json` (`json.schemas`, `yaml.schemas`
and `evenBetterToml.schema.associations`), pointing at the host's `obj/appsettings.schema.json`.

- Comments, trailing commas, indentation, line endings and a byte-order mark are preserved. Other settings and other entries
  are untouched, and when the mappings are already there the file isn't rewritten.
- Each host gets entries of its own, keyed by its schema path, so several applications can share one file. An entry that
  already maps the host's schema only gets what is missing. If you move a project, delete its old entries by hand.
- The file goes in the nearest ancestor of the project that contains a `.git` entry, or the project folder when there is
  none. `OptionaryEditorSettingsDirectory` picks another folder (relative to the project); it must contain the project,
  because VS Code resolves the schema path relative to the folder it opened.
- Only hosts write it, once per build (from the first target framework), never in design-time builds, and not when
  `OptionarySchema` is `false`.
- A file Optionary can't update safely (not valid JSON with comments, not UTF-8, an unexpected shape, read-only) is left
  alone, and the build warns with the entries to paste.
- Optionary never adds `$schema` keys or `# yaml-language-server` modelines to your configuration files.

It is off by default because it edits a file that is probably checked in. Existing mappings keep working across Optionary
versions, since entries are matched by schema path.

## JetBrains Rider

Rider uses its own JSON Schema mappings:

1. Open **Settings | Languages & Frameworks | Schemas and DTDs | JSON Schema Mappings**.
2. Add a mapping, choose `obj/appsettings.schema.json` as the schema file, set the version to Draft 7, and add the
   application's `appsettings*.json` files.
3. Rider applies JSON Schema mappings to YAML files too; add the `appsettings*.yaml` files the same way.

## Other editors

- **yaml-language-server** reads a `yaml.schemas` setting like VS Code's, or a `# yaml-language-server: $schema=...`
  modeline in the file.
- **Taplo** takes the schema from `.taplo.toml`.

## When completion doesn't work

- **No completion at all.** Build the project: the file is written after compilation. Check that the mapping's path is
  relative to the folder the editor opened, and that the file name matches `appsettings*`.
- **A key is flagged as unknown.** Check its spelling against the options type. Set `OptionarySchemaStrict` to `false` to
  stop the check.
- **A section is missing.** The application schema only covers assemblies the host references. Check the options type is
  declared and the declaration wasn't rejected (look for `OPT` diagnostics).
- **Quoted numbers are flagged.** Write the number without quotes.

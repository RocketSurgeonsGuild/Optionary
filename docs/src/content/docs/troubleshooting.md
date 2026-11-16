---
title: Troubleshooting
description: What to check when the generator doesn't run, the registration method is missing, or the schema isn't there.
tags: [msbuild, source-generator]
---

## The generator isn't loaded

The build fails with [`OPT1001`](../reference/diagnostics/#opt1001): `Optionary` is referenced, but its generator isn't
among the compiler's analyzers (`@(Analyzer)`). Nothing is bound and the registration method doesn't exist.

- **A target removes the generator.** Look for a target, in the project, a `Directory.Build.targets` or another package,
  that removes `Optionary.Analyzers` from `@(Analyzer)`, and remove it.
- **The compiler is too old.** The package carries the generator for Roslyn 5.0 and later, so an older compiler matches no
  slice and loads none. Build with the .NET 10 SDK or later, and check that `global.json` doesn't pin an older one.

`ExcludeAssets="analyzers"` on the package reference doesn't cause this: the .NET SDK passes a package's analyzers to the
compiler whatever the reference excludes, so the generator still runs.

## CS0234, CS0103 or CS1061 at a call to `AddOptionaryConfigurationOptions`

If Optionary's build assets are excluded (`ExcludeAssets="build"` or `ExcludeAssets="all"`, or an `IncludeAssets` without
`build`), its `.props` and `.targets` aren't imported, so the `OPT1001` check doesn't run either. The generator still runs,
but `RootNamespace` isn't passed to it (Optionary's `CompilerVisibleProperty`), so it puts `OptionaryConfigurationOptions`
in the global namespace. The schema targets don't run, so there's no `appsettings.schema.json` either.

A `using <RootNamespace>;` then doesn't find the method (CS1061: `IServiceCollection` has no
`AddOptionaryConfigurationOptions`), and a fully qualified call to `<RootNamespace>.OptionaryConfigurationOptions` fails
with CS0234 (or CS0103). In a Dovetail application the error is in Dovetail's generated exports file, at the joint that
calls `global::<RootNamespace>.OptionaryConfigurationOptions.AddOptionaryConfigurationOptions`: CS0234.

Include the package's `build` assets.

## The registration method isn't found, but the generator runs

- **Missing `using`.** The method is in the project's `RootNamespace`. Top-level statements are in the global namespace, so
  `Program.cs` needs `using <RootNamespace>;`.
- **Old C#.** Below C# 12 the method exists but is empty, with [`OPT0001`](../reference/diagnostics/#opt0001).
- **CS0436.** Two assemblies with the same `RootNamespace`, one with `InternalsVisibleTo` the other, both define
  `OptionaryConfigurationOptions`. Give them distinct `RootNamespace`s.

## An options type isn't bound

- Check the build for `OPT` errors: a rejected declaration contributes nothing.
- `RegisterOptions = false` only describes the section; bind the type yourself.
- Named options aren't registered: `IOptionsMonitor<T>.Get("name")` throws for any name but the default.
- Under the `Dash` or `Underscore` [key style](../guides/key-styles/), `MaxFileSizeMb` in the configuration doesn't bind;
  write `max-file-size-mb` or `max_file_size_mb`.

## No schema, or a section is missing from it

- Build the project: the file is written after compilation.
- Only hosts write `appsettings.schema.json`. A library writes `ConfigurationSchema.json` instead. Set `OptionarySchemaHost`
  to `true` if a project should be a host.
- `OptionarySchema` set to `false` turns all schema files off.
- The application schema only covers assemblies the host references, and only declarations that weren't rejected.
- Libraries packed with Dovetail 0.4 or 0.5 carry `Dovetail.ConfigurationSchema.v1:` fragments, which Optionary doesn't
  read. Rebuild them with Optionary.

[Schema and editors](../guides/schema-and-editors/#when-completion-doesnt-work) covers editor-side problems.

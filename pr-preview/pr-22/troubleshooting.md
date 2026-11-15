# Troubleshooting

## The generator isn't loaded

The build fails with [`OPT1001`](../reference/diagnostics/#opt1001): `Optionary` is referenced, but its generator isn't
among the project's analyzers. Nothing is bound and the registration method doesn't exist.

- **Analyzers are excluded.** Look for `ExcludeAssets="analyzers"` (or `all`) on the `Optionary` package reference, or in a
  `Directory.Packages.props` or `Directory.Build.props`, and remove it.
- **The compiler is too old.** The package carries the generator for Roslyn 5.0 and later. Build with the .NET 10 SDK or
  later, and check that `global.json` doesn't pin an older one.

## CS0103 or CS1061 at a call to `AddOptionaryConfigurationOptions`

If Optionary's build assets are excluded as well as its analyzers (`ExcludeAssets="build;analyzers"`, or
`IncludeAssets="compile;runtime"`), its targets don't run either, so `OPT1001` can't be reported. The first sign is the
compiler failing at the call to the registration method: CS1061 (`IServiceCollection` has no
`AddOptionaryConfigurationOptions`) or CS0103 (the name doesn't exist). In a Dovetail application the error is in Dovetail's
generated exports file, at the joint that calls the method, and is CS0234 or CS0103.

Include the package's `build` and `analyzers` assets.

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
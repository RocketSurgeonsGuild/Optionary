# Optionary

- Package: `Optionary`
- Version: `0.1.3-pr.4`
- Target framework: `net10.0`
- Source: [GitHub](https://github.com/RocketSurgeonsGuild/Optionary/tree/Branch.pull-73-merge.Sha.ab9424ae6e344d1b4ec844aa991ac51b793ffb18/src/Optionary)
- NuGet: [Optionary](https://www.nuget.org/packages/Optionary)

## Namespaces

### Optionary

- [ConfigurationKeyStyle](/Optionary/pr-preview/pr-73/api/optionary/configurationkeystyle.md) -- `enum` -- How [OptionaryAttribute](/Optionary/pr-preview/pr-73/api/optionary/optionaryattribute.md) spells the configuration keys of an options type and of every object type nested in it.
- [OptionaryAttribute](/Optionary/pr-preview/pr-73/api/optionary/optionaryattribute.md) -- `class` `sealed` -- Declares the configuration section of an options class. Optionary's source generator describes the class as JSON Schema, which is merged into the application's `appsettings.schema.json` for editor completion and validation, and registers it with the options pipeline.
- [OptionaryAttribute<T>](/Optionary/pr-preview/pr-73/api/optionary/optionaryattribute-1.md) -- `class` `sealed` -- Declares the configuration section of an options type `T` from the assembly, for types you don't own.
- [OptionaryServiceCollectionExtensions](/Optionary/pr-preview/pr-73/api/optionary/optionaryservicecollectionextensions.md) -- `class` `static` -- Registers strongly-typed configuration classes with the standard `Extensions.Options` pipeline.

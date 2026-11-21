# Optionary

- Package: `Optionary`
- Version: `0.1.3-beta.2`
- Target framework: `net10.0`
- Source: [GitHub](https://github.com/RocketSurgeonsGuild/Optionary/tree/Branch.main.Sha.5ec89e9fb7723cab52a7487ecd7afa785420bcf3/src/Optionary)
- NuGet: [Optionary](https://www.nuget.org/packages/Optionary)

## Namespaces

### Optionary

- [ConfigurationKeyStyle](/Optionary/api/optionary/configurationkeystyle.md) -- `enum` -- How [OptionaryAttribute](/Optionary/api/optionary/optionaryattribute.md) spells the configuration keys of an options type and of every object type nested in it.
- [OptionaryAttribute](/Optionary/api/optionary/optionaryattribute.md) -- `class` `sealed` -- Declares the configuration section of an options class. Optionary's source generator describes the class as JSON Schema, which is merged into the application's `appsettings.schema.json` for editor completion and validation, and registers it with the options pipeline.
- [OptionaryAttribute<T>](/Optionary/api/optionary/optionaryattribute-1.md) -- `class` `sealed` -- Declares the configuration section of an options type `T` from the assembly, for types you don't own.
- [OptionaryServiceCollectionExtensions](/Optionary/api/optionary/optionaryservicecollectionextensions.md) -- `class` `static` -- Registers strongly-typed configuration classes with the standard `Extensions.Options` pipeline.

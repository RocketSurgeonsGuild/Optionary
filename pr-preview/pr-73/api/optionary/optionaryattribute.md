# OptionaryAttribute

- Kind: `class`
- Package: [Optionary](/Optionary/pr-preview/pr-73/api/optionary.md)
- Version: `0.1.3-pr.4`
- Namespace: `Optionary`
- Target framework: `net10.0`
- Source: [GitHub](https://github.com/RocketSurgeonsGuild/Optionary/blob/Branch.pull-73-merge.Sha.ab9424ae6e344d1b4ec844aa991ac51b793ffb18/src/Optionary/OptionaryAttribute.cs)
- Inherits: `Attribute`

Declares the configuration section of an options class. Optionary's source generator describes the class as JSON Schema, which is merged into the application's `appsettings.schema.json` for editor completion and validation, and registers it with the options pipeline.

## Definition

```csharp
namespace Optionary;

public sealed class OptionaryAttribute
    : System.Attribute
{
    // ...
}
```

## Remarks

Without a section path the section is the type name with an `Options`, `Settings`, `Configuration` or `Config` suffix removed, so `GreetingsOptions` becomes `Greetings`. Nested sections use `:`, as in `"Http:Retry"`.

A type has one section, so it can be declared once. For a type you don't own, use the assembly form, `OptionaryAttribute`1`.

[OptionaryAttribute.KeyStyle](/Optionary/pr-preview/pr-73/api/optionary/optionaryattribute/properties.md#keystyle) picks how the keys of the type and of the types nested in it are spelled (as declared, Pascal, camel, dash or underscore case). A type that several sections reach has to spell its keys the same way in all of them; OPT0011 reports a mismatch.

The generated registration reads its own section from the application's complete `IConfiguration` and creates or binds the options.

Only works with source generators enabled.

## Constructors

- [OptionaryAttribute](/Optionary/pr-preview/pr-73/api/optionary/optionaryattribute/constructors.md#constructor) -- Declares the attributed class under the section derived from its name.
- [OptionaryAttribute(string)](/Optionary/pr-preview/pr-73/api/optionary/optionaryattribute/constructors.md#constructor-string) -- Declares the attributed class under `section`.

## Properties

- [KeyStyle](/Optionary/pr-preview/pr-73/api/optionary/optionaryattribute/properties.md#keystyle) : [ConfigurationKeyStyle](/Optionary/pr-preview/pr-73/api/optionary/configurationkeystyle.md) `get; set` -- How the configuration keys are spelled: those of every bound property and constructor parameter of the options type and of the object types nested in it, and the default section path derived from the type name. The default is [ConfigurationKeyStyle.Undefined](/Optionary/pr-preview/pr-73/api/optionary/configurationkeystyle/fields.md), which keeps the declared names. See [ConfigurationKeyStyle](/Optionary/pr-preview/pr-73/api/optionary/configurationkeystyle.md).
- [RegisterOptions](/Optionary/pr-preview/pr-73/api/optionary/optionaryattribute/properties.md#registeroptions) : `bool` `get; set` -- Whether to register `IOptions<T>` for the type. `false` only describes the section in the schema. The default is `true`.
- [Section](/Optionary/pr-preview/pr-73/api/optionary/optionaryattribute/properties.md#section) : `string?` `get` -- The section path, or `null` for the one derived from the type name.
- [ValidateOnStart](/Optionary/pr-preview/pr-73/api/optionary/optionaryattribute/properties.md#validateonstart) : `bool` `get; set` -- Whether to validate the options when the host starts. The default is `false`.

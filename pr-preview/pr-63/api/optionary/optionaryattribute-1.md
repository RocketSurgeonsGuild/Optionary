# OptionaryAttribute<T>

- Kind: `class`
- Package: [Optionary](/Optionary/pr-preview/pr-63/api/optionary.md)
- Version: `0.1.2-pr.18`
- Namespace: `Optionary`
- Target framework: `net10.0`
- Source: [GitHub](https://github.com/RocketSurgeonsGuild/Optionary/blob/Branch.pull-63-merge.Sha.90865672bcb9ffa17b52ca7ce0ae6db00ffc494a/src/Optionary/OptionaryAttribute.cs)
- Inherits: `Attribute`

Declares the configuration section of an options type `T` from the assembly, for types you don't own.

## Definition

```csharp
namespace Optionary;

public sealed class OptionaryAttribute<T>
    : System.Attribute
    where T : class
{
    // ...
}
```

## Type Parameters

- `T` -- The options type.

## Remarks

For example, `[assembly: Optionary<HostOptions>("Hosting")]`.

The section path follows the same rules as [OptionaryAttribute](/Optionary/pr-preview/pr-63/api/optionary/optionaryattribute.md). Apply it once for each type you configure. A type has one section: naming it twice, or also declaring it with the class form, is reported as OPT0010. `OptionaryAttribute`1.KeyStyle` works as it does on the class form.

Only works with source generators enabled.

## Constructors

- [OptionaryAttribute](/Optionary/pr-preview/pr-63/api/optionary/optionaryattribute-1/constructors.md#constructor) -- Declares `T` under the section derived from its name.
- [OptionaryAttribute(string)](/Optionary/pr-preview/pr-63/api/optionary/optionaryattribute-1/constructors.md#constructor-string) -- Declares `T` under `section`.

## Properties

- [KeyStyle](/Optionary/pr-preview/pr-63/api/optionary/optionaryattribute-1/properties.md#keystyle) : [ConfigurationKeyStyle](/Optionary/pr-preview/pr-63/api/optionary/configurationkeystyle.md) `get; set` -- How the configuration keys are spelled: those of every bound property and constructor parameter of the options type and of the object types nested in it, and the default section path derived from the type name. The default is [ConfigurationKeyStyle.Undefined](/Optionary/pr-preview/pr-63/api/optionary/configurationkeystyle/fields.md), which keeps the declared names. See [ConfigurationKeyStyle](/Optionary/pr-preview/pr-63/api/optionary/configurationkeystyle.md).
- [RegisterOptions](/Optionary/pr-preview/pr-63/api/optionary/optionaryattribute-1/properties.md#registeroptions) : `bool` `get; set` -- Whether to register `IOptions<T>` for the type. `false` only describes the section in the schema. The default is `true`.
- [Section](/Optionary/pr-preview/pr-63/api/optionary/optionaryattribute-1/properties.md#section) : `string?` `get` -- The section path, or `null` for the one derived from the type name.
- [ValidateOnStart](/Optionary/pr-preview/pr-63/api/optionary/optionaryattribute-1/properties.md#validateonstart) : `bool` `get; set` -- Whether to validate the options when the host starts. The default is `false`.

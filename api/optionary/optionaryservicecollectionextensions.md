# OptionaryServiceCollectionExtensions

- Kind: `class`
- Package: [Optionary](/Optionary/api/optionary.md)
- Version: `0.1.2-beta.24`
- Namespace: `Optionary`
- Target framework: `net10.0`
- Source: [GitHub](https://github.com/RocketSurgeonsGuild/Optionary/blob/Branch.main.Sha.402f98362a572264fafc51bc86840ddc134bf2cc/src/Optionary/OptionaryServiceCollectionExtensions.cs)

Registers strongly-typed configuration classes with the standard `Extensions.Options` pipeline.

## Definition

```csharp
namespace Optionary;

public static class OptionaryServiceCollectionExtensions
{
    // ...
}
```

## Remarks

The registration method Optionary's source generator emits for `[Optionary]` types calls [OptionaryServiceCollectionExtensions.AddOptionaryOptions(IServiceCollection, string, Func<IConfiguration, TOptions>)](/Optionary/api/optionary/optionaryservicecollectionextensions/methods.md#addoptionaryoptions-iservicecollection-string-func-iconfiguration-toptions) for every type, passing the `Create...` method it generated for it. That code creates the instance and binds the section onto it without reflection, so the registration is trimming and AOT safe, and nothing here uses reflection. A library registers only its own section, and reads it from the application's complete `Configuration.IConfiguration`, which the service provider supplies when the options are first created.

## Methods

- [AddOptionaryOptions(IServiceCollection, string, Func<IConfiguration, TOptions>)](/Optionary/api/optionary/optionaryservicecollectionextensions/methods.md#addoptionaryoptions-iservicecollection-string-func-iconfiguration-toptions) : `OptionsBuilder<TOptions>` `extension` -- Creates `TOptions` from the configuration section at `sectionPath` with `create`, without reflection.

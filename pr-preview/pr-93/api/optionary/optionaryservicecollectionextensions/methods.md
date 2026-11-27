# OptionaryServiceCollectionExtensions Methods

- Package: [Optionary](/Optionary/pr-preview/pr-93/api/optionary.md)
- Type: [OptionaryServiceCollectionExtensions](/Optionary/pr-preview/pr-93/api/optionary/optionaryservicecollectionextensions.md)
- Kind: `Methods`
- Members: `1`

Registers strongly-typed configuration classes with the standard `Extensions.Options` pipeline.

<a id="addoptionaryoptions"></a>
<a id="addoptionaryoptions-iservicecollection-string-func-iconfiguration-toptions"></a>

## AddOptionaryOptions(IServiceCollection, string, Func<IConfiguration, TOptions>)

- Name: `AddOptionaryOptions(IServiceCollection, string, Func<IConfiguration, TOptions>)`
- Modifiers: `extension`
- Returns: `OptionsBuilder<TOptions>`
- Source: [GitHub](https://github.com/RocketSurgeonsGuild/Optionary/blob/Branch.pull-93-merge.Sha.c88e771ee3110e8c3368ebfde4a3c890e82f9b43/src/Optionary/OptionaryServiceCollectionExtensions.cs#L74-L99)

Creates `TOptions` from the configuration section at `sectionPath` with `create`, without reflection.

```csharp
public static class OptionaryServiceCollectionExtensions
{
    public static OptionsBuilder<TOptions> AddOptionaryOptions<TOptions>(
        this IServiceCollection services,
        string sectionPath,
        Func<IConfiguration, TOptions> create)
    {
        // ...
    }
}
```

## Parameters

- `services` (`IServiceCollection`)
  The service collection to register against.
- `sectionPath` (`string`)
  The path of the configuration section to create from, such as `Server` or `Http:Retry`.
- `create` (`Func<IConfiguration, TOptions>`)
  Creates an instance from a configuration section.

## Returns

`OptionsBuilder<TOptions>` -- The `Options.OptionsBuilder`1` for the default name, for further configuration such as `Validate` and `ValidateOnStart()`.

## Exceptions

- `ArgumentNullException` -- `services`, `sectionPath`, or `create` is `null`.
- `ArgumentException` -- `sectionPath` is empty.

## Remarks

This is the one registration the generated registration method emits, for every `[Optionary]` type. It registers an `Options.IOptionsFactory`1` that creates each instance with `create`, unless a factory is already registered for `TOptions`. That is a `TryAdd`, so an `Options.IOptionsFactory`1` the application registered itself wins over this one, and `create` is never called for that type. Registered before this call, the application's factory makes this registration a no-op. Registered after it with a plain `Add`, it is the last registration, which the container resolves. The factory derives from `Options.OptionsFactory`1`, so configure and post-configure actions and validators still run on the instance `create` returns.

A type is created from one section only. When several calls register a section for the same type, the first one wins and the rest are ignored, as with any other `TryAdd` registration.

The configuration is the application's `Configuration.IConfiguration`, which the service provider supplies when the options are first created. `create` is called with the section at `sectionPath`. A change token source watches that configuration, so `Options.IOptionsMonitor`1` and `Options.IOptionsSnapshot`1` observe reloads. Optionary only registers the default options: asking for any other options name throws `InvalidOperationException`.

On .NET 11, don't give a type registered this way asynchronous validators: the options pipeline only runs them through the built-in `Options.OptionsFactory`1` itself, and through a derived factory they fail synchronous validation.

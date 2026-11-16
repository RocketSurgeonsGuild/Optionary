---
title: Registering from a library
description: The registration method Optionary generates into every assembly, its contract, and how a library exposes it.
tags: [di, source-generator, api]
---

Optionary generates a **registration method** into every assembly that uses the `[Optionary]` attribute. It registers that
assembly's options types with the options pipeline. It is internal on purpose, so a library calls it from its own public
extension method and the method never becomes part of the library's public API.

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace Contoso.Greetings; // the library's RootNamespace

public static class GreetingsServiceCollectionExtensions
{
    public static IServiceCollection AddGreetings(this IServiceCollection services)
    {
        services.AddOptionaryConfigurationOptions(); // generated, internal, in Contoso.Greetings
        services.AddSingleton<Greeter>();
        return services;
    }
}
```

An application does the same from its own startup code: see [getting started](../../getting-started/#register-it).

## The contract

The method's name, its class, its signature and the rules below are public API. They change only in a major version, and
host frameworks rely on them: Dovetail, for one, calls the method from a joint it generates when it finds `[Optionary]`
declarations.

```csharp
namespace <RootNamespace>;

[global::System.CodeDom.Compiler.GeneratedCode("Optionary", "<version>")]
internal static class OptionaryConfigurationOptions
{
    internal static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddOptionaryConfigurationOptions(
        this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services);
}
```

- **File.** The hint name is `Optionary.Options.g.cs`.
- **Always emitted when the attribute type is present.** The method exists in every compilation where
  `Optionary.OptionaryAttribute` resolves to the type in the assembly named `Optionary`, so a call to it always compiles:
    - with no declarations at all;
    - when every declaration was rejected;
    - when every declaration has `RegisterOptions = false`;
    - when the C# version is older than 12: the body is empty and [`OPT0001`](../../reference/diagnostics/#opt0001) is
      reported.

    A class of your own named `Optionary.OptionaryAttribute` in another assembly doesn't count, and nothing is emitted for it.

- **The `RootNamespace` rule.** The class goes in the project's `RootNamespace` build property, used verbatim. When
  `RootNamespace` is missing, empty or whitespace, the class is in the global namespace. Optionary's build assets make
  `RootNamespace` visible to the generator.
- **What it registers.** One `AddOptionaryOptions<T>(services, "<section path>", create)` call per registered options type,
  ordered by section path (ignoring case, then ordinal) and then type name. A declaration with `ValidateOnStart = true` gets
  `ValidateOnStart()`, and a `[Validate]` type gets `ValidateWithZeroAlloc()` (see [validation](../binding/#validation)).
  Then every `[OptionsValidator]` class of the assembly for those types, with `TryAddEnumerable`.
- **Repeat calls are harmless.** Every registration is a `TryAdd` or `TryAddEnumerable`, so a library's own `AddGreetings()`
  and a host framework can both call it.
- **It returns `services`**, for chaining.

## Two assemblies with the same `RootNamespace`

Each assembly's class is internal, so two assemblies normally don't see each other's. If one has `InternalsVisibleTo` the
other and both have the same `RootNamespace`, the consumer gets warning CS0436 and the definition in its own source wins.
Give the projects distinct `RootNamespace`s.

## If the method is missing

A call to `AddOptionaryConfigurationOptions` that doesn't compile (CS1061, CS0234 or CS0103) usually means Optionary's build
assets are excluded: the generator still runs, but without `RootNamespace`, so the method is in the global namespace. See
[troubleshooting](../../troubleshooting/).

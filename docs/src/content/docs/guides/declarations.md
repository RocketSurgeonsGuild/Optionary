---
title: Declarations
description: Mark an options type with its section, on the type or on the assembly, and how the section path is derived.
tags: [configuration, source-generator]
---

A **declaration** marks an **options type** with its **section**. There are two forms, and a type has exactly one
section, so it is declared once.

## On the type: `[Optionary]`

Put `[Optionary]` on a class or record you own:

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Optionary;

/// <summary>Options for the greetings this library prints.</summary>
[Optionary]
public sealed class GreetingsOptions
{
    /// <summary>What the greeting says before the name.</summary>
    [Description("The text that comes before the name, e.g. \"Hello\".")]
    public string Message { get; set; } = "Hello";

    /// <summary>How many times the greeting is said.</summary>
    [Range(1, 5)]
    public int Repeat { get; set; } = 1;
}
```

Records and other immutable types work too; see [binding](../binding/#records-and-other-immutable-types):

```csharp
[Optionary]
public sealed record FarewellOptions(string Message, [property: Range(0, 5)] int Exclamations);
```

`[Optionary]` can't be repeated on a type.

## On the assembly: `[assembly: Optionary<T>]`

To declare a type you don't own, use the generic attribute on the assembly. This declares `HostOptions` under `Hosting`:

```csharp
[assembly: Optionary<HostOptions>("Hosting")]
```

Repeat the attribute for each type. Descriptions for a type from a package come from the XML documentation file that ships
with the package, when there is one (see `OptionaryReferenceDocumentation` in the
[MSBuild reference](../../reference/msbuild-properties/)).

## The section path

| Declaration                           | Section path                                                                                                                                                                           |
| ------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `[Optionary]`                         | The type name with one `Configuration`, `Settings`, `Options` or `Config` suffix removed, spelled in the declaration's [key style](../key-styles/): `GreetingsOptions` is `Greetings`. |
| `[Optionary("Http:Retry")]`           | The path you give, used as written. `:` nests sections.                                                                                                                                |
| `[assembly: Optionary<T>]`            | The same rule as the class form, applied to `T`.                                                                                                                                       |
| `[assembly: Optionary<T>("Hosting")]` | The path you give.                                                                                                                                                                     |

- There is no prefix: a library's sections sit at the configuration root unless you give a path.
- Configuration keys ignore case, so `Greetings` and `greetings` are the same section.
- The suffix is matched case-sensitively, and only one is removed.
- A path that is empty, has an empty segment (`A::B`), or a type name that would be empty without its suffix (a class called
  `Options`) is rejected with [`OPT0003`](../../reference/diagnostics/#opt0003). Give it a path.

## What can be declared

An options type is a class or record class that configuration can create. These are rejected with
[`OPT0002`](../../reference/diagnostics/#opt0002): interfaces, static and abstract classes, structs, enums, open generic
types and collections. A type that can't be created from configuration (no public constructor, several public constructors
with parameters and none without, or a constructor parameter that matches no property) is rejected with
[`OPT0006`](../../reference/diagnostics/#opt0006).

A rejected declaration is reported and contributes nothing: no fragment and no registration. The assembly still gets its
[registration method](../registering-from-a-library/).

## One section per type

- `[Optionary]` and `[assembly: Optionary<T>]` for the same type is an error,
  [`OPT0010`](../../reference/diagnostics/#opt0010), reported on the assembly attribute. The class declaration is the one
  that produces the fragment and the registration; the assembly attribute produces nothing.
- Naming the same `T` in two assembly attributes is `OPT0010` too.

At run time each assembly reads only its own sections from the application's complete `IConfiguration`. Optionary doesn't
order, de-duplicate or merge sections at run time; the registration method registers an assembly's types by section path,
then type name, and the standard options pipeline does the rest.

## Attribute properties

Both forms have the same members.

| Member            | Default     | Effect                                                                                                                                                   |
| ----------------- | ----------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Section`         | derived     | The section path, from the constructor argument. Left out, it is derived from the type name.                                                             |
| `KeyStyle`        | `Undefined` | How keys are spelled. See [key styles](../key-styles/).                                                                                                  |
| `RegisterOptions` | `true`      | `false` only describes the section in the schema. Nothing is registered, so you bind the type yourself (see [binding](../binding/#registering-by-hand)). |
| `ValidateOnStart` | `false`     | Validates the options when the host starts, instead of the first time they are read.                                                                     |

The attributes are `[Conditional("CodeGeneration")]`: the generator reads them from source, and they never reach the
compiled assembly's metadata.

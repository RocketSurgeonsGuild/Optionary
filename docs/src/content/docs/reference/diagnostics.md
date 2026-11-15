---
title: Diagnostics
description: The OPT diagnostics the generator and the build report, and how to fix each.
tags: [source-generator, msbuild]
---

The generator reports `OPT0001` to `OPT0012`, in the `Optionary` category. `OPT1001` is an MSBuild error from Optionary's
targets. The generator runs in every project that references `Optionary`, including through a transitive reference, so these
can fire in a consumer of a library too. They are reported against source, so `#pragma warning disable` works for the
warnings.

| Id                    | Severity | Summary                                                  |
| --------------------- | -------- | -------------------------------------------------------- |
| [`OPT0001`](#opt0001) | Warning  | C# older than 12: nothing is bound                       |
| [`OPT0002`](#opt0002) | Error    | Invalid options type                                     |
| [`OPT0003`](#opt0003) | Error    | Invalid section path                                     |
| [`OPT0004`](#opt0004) | Warning  | Unsupported member                                       |
| [`OPT0005`](#opt0005) | Warning  | Key collision                                            |
| [`OPT0006`](#opt0006) | Error    | Options type can't be created                            |
| [`OPT0007`](#opt0007) | Warning  | Type graph too large                                     |
| [`OPT0008`](#opt0008) | Warning  | Annotation not expressible in JSON Schema                |
| [`OPT0009`](#opt0009) | Warning  | Schema generation failed                                 |
| [`OPT0010`](#opt0010) | Error    | Options type declared more than once                     |
| [`OPT0011`](#opt0011) | Error    | Key style conflict                                       |
| [`OPT0012`](#opt0012) | Warning  | ZeroAlloc options validation skipped                     |
| [`OPT1001`](#opt1001) | Error    | The runtime is referenced but the generator isn't loaded |

## OPT0001

**The project's C# version is older than 12.** The [registration method](../../guides/registering-from-a-library/) is still
emitted, so calls to it compile, but its body is empty: no options type is registered and no fragment is written. Set
`LangVersion` to 12 or later.

## OPT0002

**The type can't be described as configuration:** an interface, a static or abstract class, a struct or enum, an open
generic type, or a collection. Declare a class or record class instead.

## OPT0003

**The section path isn't valid for the type:** it is empty, has an empty segment (`A::B`), or was derived from a type name
that is only a suffix, such as a class called `Options`. Give the declaration a path:
`[Optionary("Section")]`.

## OPT0004

**A member has a type configuration binding doesn't support.** It is left out of the schema and the binder. A `required`
member that can't be bound is set to `default!` so the generated code compiles. See
[supported types](../../guides/binding/#supported-types).

## OPT0005

**Two members bind the same key.** Keys ignore case, so `Name` and `NAME` collide, as do two members given the same
`[ConfigurationKeyName]`. Only the first is described.

## OPT0006

**The type can't be created from configuration:** it has no public constructor, several public constructors with
parameters and none without, or a constructor parameter that matches no property.

## OPT0007

**The type graph uses too many distinct types** (more than 200), so the schema stops describing them at the member named.
Self-expanding generic types report this too.

## OPT0008

**An annotation has no JSON Schema equivalent** (a regular expression that can't be anchored, for example) and is left out
of the schema. The member is still bound.

## OPT0009

**Schema generation failed unexpectedly for a type.** The type has no schema. Please
[report it](https://github.com/RocketSurgeonsGuild/Optionary/issues).

## OPT0010

**A type is declared more than once:** by both `[Optionary]` on the class and `[assembly: Optionary<T>]`, or by two
assembly attributes. It is reported on the assembly attribute, which contributes nothing. Keep one declaration.

## OPT0011

**A type is bound with more than one key style**, because sections with different key styles reach it. The later
declaration is reported and contributes nothing. Use the same `KeyStyle` for every section that reaches the type, or give
the shared type `[ConfigurationKeyName]` attributes. See [key styles](../../guides/key-styles/#one-spelling-per-type).

## OPT0012

**A `[Validate]` type has an asynchronous ZeroAlloc.Validation rule**, so it isn't validated with
`ValidateWithZeroAlloc()`: options validation is synchronous. Make the rule synchronous, or validate the type another way.

## OPT1001

**`Optionary` is referenced but its generator isn't loaded.** Reported by the `_OptionaryCheckGenerator` target in
`Optionary.targets`, before `CoreCompile`, when `Optionary.dll` is among the references and no analyzer named
`Optionary.Analyzers` is. Without the generator nothing is bound and no registration method exists.

It happens when:

- the package reference excludes analyzers, for example `ExcludeAssets="analyzers"`; or
- the compiler is older than Roslyn 5.0, for which the package has no generator.

Remove the exclusion, or build with the .NET 10 SDK or later. See [troubleshooting](../../troubleshooting/).

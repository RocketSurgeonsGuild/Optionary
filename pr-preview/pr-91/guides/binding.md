# Binding and validation

Every declared options type is created and bound by generated code and registered through an `IOptionsFactory<T>`.
Nothing uses reflection, so registration is trimming and Native AOT safe, and your own `Configure<T>`, `PostConfigure<T>`
and validators still run on the created instance, whenever they were registered.

## The generated binder

Optionary doesn't call Microsoft's configuration binding source generator: a source generator never sees another
generator's output, so calls in Optionary's own generated code would not be intercepted. Instead it **ports** that
generator (the parser, the type specifications and the emitter from dotnet/runtime's
`Microsoft.Extensions.Configuration.Binder/gen`, MIT), with both its version 10 and version 11 rules.

The rules follow the **major version of `Microsoft.Extensions.Configuration.Binder`** the project references, not its
target framework, because a `net10.0` application can reference Binder 11. A project that references no Binder gets the
version 11 rules.

### Binder 10 and Binder 11

| Case                                           | Binder 10              | Binder 11                    |
| ---------------------------------------------- | ---------------------- | ---------------------------- |
| `Nullable<T>` set to `""`                      | value kept             | `null`                       |
| String or object constructor parameter missing | throws                 | `null`                       |
| Value-type constructor parameter set to `""`   | default                | throws                       |
| Nullable or reference-type parameter missing   | throws                 | `null` (structs still throw) |
| Parameter with a default, set to null          | `null`                 | the default                  |
| Properties bound through the constructor       | bound again afterwards | not bound again              |
| `[ConfigurationIgnore]`                        | ignored                | honoured, in the schema too  |

The same data is bound three ways in the parity tests (Microsoft's generator, the reflection binder and Optionary's) on
both versions.

### Where Optionary differs from Microsoft's generator, on purpose

1. **Object-initializer types.** Microsoft's generator never sets `init`-only members of a type with a parameterless
   constructor, and can't create a type with `required` members (CS9035). Optionary creates them with an object
   initializer, then binds the rest. A missing key keeps the member's own initial value, as the reflection binder does; a
   `required` member with no value gets `default`.
2. **`required` members that can't be bound** (an unsupported type, or `[ConfigurationIgnore]`) are set to `default!`, so
   the generated code compiles. [`OPT0004`](../../reference/diagnostics/#opt0004) reports the member.
3. **Empty sections still produce an instance.** `Get<T>()` returns `null` for a missing section, but `IOptions<T>.Value`
   can't be `null`. A missing required constructor parameter throws the first time the options are read.
4. **No `NotSupportedException`s**, and no `BindNonPublicProperties`.
5. **Self-expanding generics**, such as `class Node<T> { Node<Node<T>> Next; }`, are treated as unsupported past a nesting
   depth of 10 or 1,000 types, instead of making the analysis run forever. The schema reports
   [`OPT0007`](../../reference/diagnostics/#opt0007) for the same graphs.

Microsoft's own quirks are kept: a positional record's extra `init`-only property is set to `default` when its key is
missing, overriding its initializer, and Binder 10 binds constructor-bound collections twice.

Registration also differs from `services.Configure<T>(section)`: the configuration's reload token feeds
`IOptionsMonitor<T>` through one change token source per type, and the `IConfiguration` is resolved from the container
when the options are first created, not captured at registration.

## Supported types

The binder follows Microsoft's binding generator, not the JSON serializer:

- `string`, `bool`, every numeric type (including `Half`, `Int128` and `UInt128`), `char`, enums (any casing), `TimeSpan`,
  `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `Guid`, `Version`, `Uri`, `CultureInfo` and `byte[]`;
- `Nullable<T>` of those;
- nested classes, structs and records, and recursive types (the schema uses `$ref`);
- arrays, lists, sets and the other collection types the binder supports, and dictionaries with string, enum or integer
  keys;
- `object` and `IConfigurationSection`, which accept any value.

A type with a `[TypeConverter]` is described as an object, because the binder ignores converters. A member the binder
can't bind is left out of both the schema and the binder, with warning [`OPT0004`](../../reference/diagnostics/#opt0004).
`[ConfigurationKeyName]` renames a key, and from Binder 11 `[ConfigurationIgnore]` leaves a member out.

## Records and other immutable types

Types the options pipeline can't create itself (constructor parameters, `init`-only or `required` members) are registered
like any other: the generated code builds the instance from the section, and your `Configure<T>` and validators still run
on it.

```csharp
[Optionary]
public sealed record FarewellOptions(string Message, [property: Range(0, 5)] int Exclamations);
```

## How options are registered

The [registration method](../registering-from-a-library/) calls one public method per options type,
`OptionaryServiceCollectionExtensions.AddOptionaryOptions<TOptions>`, passing the create function it generated for the
type. It is also yours to call, for example for a type declared with `RegisterOptions = false`.

### Registering by hand

```csharp
services.AddOptionaryOptions<GreetingsOptions>("Greetings", static configuration => new GreetingsOptions
{
    Message = configuration["Message"] ?? "Hello",
});
```

`AddOptionaryOptions<TOptions>(this IServiceCollection services, string sectionPath, Func<IConfiguration, TOptions> create)`
returns the `OptionsBuilder<TOptions>`, so `Validate` and `ValidateOnStart()` chain as usual.

- The configuration is the application's complete `IConfiguration`, resolved from the service provider when the options are
  first created. `create` receives its section at `sectionPath` (such as `Http:Retry`). An empty path throws.
- A change token source watches the configuration, so `IOptionsMonitor<T>` and `IOptionsSnapshot<T>` see reloads.
- The call registers an `IOptionsFactory<T>` with `TryAdd`: the first registration for a type wins and later calls are
  ignored. A factory your application registers itself wins over Optionary's, and `create` is never called for that type.
- The factory derives from `OptionsFactory<T>`, so configure and post-configure actions and validators still run on the
  instance `create` returns. On .NET 11, don't give such a type asynchronous validators: they fail synchronous validation
  through a derived factory.

There is one overload because everything is created through generated code. To bind a type with reflection, use
Microsoft's `Configure<T>(IConfiguration)` or `AddOptions<T>().Bind(...)` yourself.

### Named options

Optionary registers the default options only. Reading any other name, such as `IOptionsMonitor<T>.Get("tenant-a")`,
throws an `InvalidOperationException` naming the type and the options name.

## Validation

Optionary never turns on `ValidateDataAnnotations`. `[Range]`, `[MinLength]` and the other annotations describe the
[schema](../schema-and-editors/), which is how editors flag a bad value; they are not enforced at run time.

To enforce rules, write an `[OptionsValidator]` class, the options validation source generator from
`Microsoft.Extensions.Options`:

```csharp
[OptionsValidator]
public sealed partial class GreetingsOptionsValidator : IValidateOptions<GreetingsOptions>;
```

The registration method registers every such validator in the assembly for the options types it registers, with
`TryAddEnumerable`, so it never duplicates one you added yourself. Set `ValidateOnStart = true` on the declaration and a bad
configuration fails the host at startup.

### ZeroAlloc.Validation

With [ZeroAlloc.Validation](https://validation.zeroalloc.net/docs/options) and `ZeroAlloc.Validation.Options` referenced,
mark the options type `[Validate]` instead: the registration method calls the `ValidateWithZeroAlloc()` overload that
ZeroAlloc.Validation generates for it.

- It applies to `[Validate]` classes of the same assembly that aren't private or protected (nor nested in a type that is).
  `ZeroAllocGeneratedAccessibility=Internal` is honoured.
- A type with an asynchronous rule isn't validated this way, because options validation is synchronous; it is reported
  with [`OPT0012`](../../reference/diagnostics/#opt0012).
- Set `OptionaryZeroAllocValidation` to `false` to leave ZeroAlloc.Validation's registrations to the project.
- The rules are also written into the [schema](../schema-and-editors/#zeroallocvalidation-rules), whether or not the type is
  validated this way, so editors flag a bad value before the application starts.
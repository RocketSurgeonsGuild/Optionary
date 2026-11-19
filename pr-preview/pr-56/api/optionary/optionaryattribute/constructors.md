# OptionaryAttribute Constructors

- Package: [Optionary](/Optionary/pr-preview/pr-56/api/optionary.md)
- Type: [OptionaryAttribute](/Optionary/pr-preview/pr-56/api/optionary/optionaryattribute.md)
- Kind: `Constructors`
- Members: `2`

Declares the configuration section of an options class. Optionary's source generator describes the class as JSON Schema, which is merged into the application's `appsettings.schema.json` for editor completion and validation, and registers it with the options pipeline.

<a id="constructor"></a>

## OptionaryAttribute

- Name: `Constructor`
- Source: [GitHub](https://github.com/RocketSurgeonsGuild/Optionary/blob/Branch.pull-56-merge.Sha.f5f261856ff18feb44b994d71ba2ad6b2829e5b1/src/Optionary/OptionaryAttribute.cs#L39-L56)

Declares the attributed class under the section derived from its name.

```csharp
public sealed class OptionaryAttribute
{
    public OptionaryAttribute()
    {
        // ...
    }
}
```

<a id="constructor-string"></a>

## OptionaryAttribute(string)

- Name: `Constructor(string)`
- Source: [GitHub](https://github.com/RocketSurgeonsGuild/Optionary/blob/Branch.pull-56-merge.Sha.f5f261856ff18feb44b994d71ba2ad6b2829e5b1/src/Optionary/OptionaryAttribute.cs#L45-L56)

Declares the attributed class under `section`.

```csharp
public sealed class OptionaryAttribute
{
    public OptionaryAttribute(
        string section)
    {
        // ...
    }
}
```

## Parameters

- `section` (`string`)
  The section path, e.g. `"Greetings"` or `"Http:Retry"`.

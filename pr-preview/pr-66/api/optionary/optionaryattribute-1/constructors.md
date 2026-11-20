# OptionaryAttribute<T> Constructors

- Package: [Optionary](/Optionary/pr-preview/pr-66/api/optionary.md)
- Type: [OptionaryAttribute<T>](/Optionary/pr-preview/pr-66/api/optionary/optionaryattribute-1.md)
- Kind: `Constructors`
- Members: `2`

Declares the configuration section of an options type `T` from the assembly, for types you don't own.

<a id="constructor"></a>

## OptionaryAttribute

- Name: `Constructor`
- Source: [GitHub](https://github.com/RocketSurgeonsGuild/Optionary/blob/Branch.pull-66-merge.Sha.a589b7ab87d95b43a6d10acf0236e0e514b6322f/src/Optionary/OptionaryAttribute.cs#L39-L56)

Declares `T` under the section derived from its name.

```csharp
public sealed class OptionaryAttribute<T>
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
- Source: [GitHub](https://github.com/RocketSurgeonsGuild/Optionary/blob/Branch.pull-66-merge.Sha.a589b7ab87d95b43a6d10acf0236e0e514b6322f/src/Optionary/OptionaryAttribute.cs#L45-L56)

Declares `T` under `section`.

```csharp
public sealed class OptionaryAttribute<T>
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

# OptionaryAttribute<T> Constructors

- Package: [Optionary](/Optionary/pr-preview/pr-67/api/optionary.md)
- Type: [OptionaryAttribute<T>](/Optionary/pr-preview/pr-67/api/optionary/optionaryattribute-1.md)
- Kind: `Constructors`
- Members: `2`

Declares the configuration section of an options type `T` from the assembly, for types you don't own.

<a id="constructor"></a>

## OptionaryAttribute

- Name: `Constructor`
- Source: [GitHub](https://github.com/RocketSurgeonsGuild/Optionary/blob/Branch.pull-67-merge.Sha.44523ab3556f426ebd4d5d9d6e3154d9ea36b231/src/Optionary/OptionaryAttribute.cs#L39-L56)

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
- Source: [GitHub](https://github.com/RocketSurgeonsGuild/Optionary/blob/Branch.pull-67-merge.Sha.44523ab3556f426ebd4d5d9d6e3154d9ea36b231/src/Optionary/OptionaryAttribute.cs#L45-L56)

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

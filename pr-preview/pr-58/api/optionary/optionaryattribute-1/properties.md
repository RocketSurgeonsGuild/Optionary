# OptionaryAttribute<T> Properties

- Package: [Optionary](/Optionary/pr-preview/pr-58/api/optionary.md)
- Type: [OptionaryAttribute<T>](/Optionary/pr-preview/pr-58/api/optionary/optionaryattribute-1.md)
- Kind: `Properties`
- Members: `4`

Declares the configuration section of an options type `T` from the assembly, for types you don't own.

<a id="keystyle"></a>

## KeyStyle

- Name: `KeyStyle`
- Modifiers: `get; set`
- Returns: [ConfigurationKeyStyle](/Optionary/pr-preview/pr-58/api/optionary/configurationkeystyle.md)
- Source: [GitHub](https://github.com/RocketSurgeonsGuild/Optionary/blob/Branch.pull-58-merge.Sha.663813de8e5c2ef2830b99a0f04924b906e30355/src/Optionary/OptionaryAttribute.cs)

How the configuration keys are spelled: those of every bound property and constructor parameter of the options type and of the object types nested in it, and the default section path derived from the type name. The default is [ConfigurationKeyStyle.Undefined](/Optionary/pr-preview/pr-58/api/optionary/configurationkeystyle/fields.md), which keeps the declared names. See [ConfigurationKeyStyle](/Optionary/pr-preview/pr-58/api/optionary/configurationkeystyle.md).

```csharp
public ConfigurationKeyStyle KeyStyle { get; set; }
```

<a id="registeroptions"></a>

## RegisterOptions

- Name: `RegisterOptions`
- Modifiers: `get; set`
- Returns: `bool`
- Source: [GitHub](https://github.com/RocketSurgeonsGuild/Optionary/blob/Branch.pull-58-merge.Sha.663813de8e5c2ef2830b99a0f04924b906e30355/src/Optionary/OptionaryAttribute.cs)

Whether to register `IOptions<T>` for the type. `false` only describes the section in the schema. The default is `true`.

```csharp
public bool RegisterOptions { get; set; }
```

<a id="section"></a>

## Section

- Name: `Section`
- Modifiers: `nullable` `get`
- Returns: `string?`
- Source: [GitHub](https://github.com/RocketSurgeonsGuild/Optionary/blob/Branch.pull-58-merge.Sha.663813de8e5c2ef2830b99a0f04924b906e30355/src/Optionary/OptionaryAttribute.cs#L50)

The section path, or `null` for the one derived from the type name.

```csharp
public string? Section { get; }
```

<a id="validateonstart"></a>

## ValidateOnStart

- Name: `ValidateOnStart`
- Modifiers: `get; set`
- Returns: `bool`
- Source: [GitHub](https://github.com/RocketSurgeonsGuild/Optionary/blob/Branch.pull-58-merge.Sha.663813de8e5c2ef2830b99a0f04924b906e30355/src/Optionary/OptionaryAttribute.cs)

Whether to validate the options when the host starts. The default is `false`.

```csharp
public bool ValidateOnStart { get; set; }
```

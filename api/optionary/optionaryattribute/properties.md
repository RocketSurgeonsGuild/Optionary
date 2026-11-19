# OptionaryAttribute Properties

- Package: [Optionary](/Optionary/api/optionary.md)
- Type: [OptionaryAttribute](/Optionary/api/optionary/optionaryattribute.md)
- Kind: `Properties`
- Members: `4`

Declares the configuration section of an options class. Optionary's source generator describes the class as JSON Schema, which is merged into the application's `appsettings.schema.json` for editor completion and validation, and registers it with the options pipeline.

<a id="keystyle"></a>

## KeyStyle

- Name: `KeyStyle`
- Modifiers: `get; set`
- Returns: [ConfigurationKeyStyle](/Optionary/api/optionary/configurationkeystyle.md)
- Source: [GitHub](https://github.com/RocketSurgeonsGuild/Optionary/blob/Branch.main.Sha.3de159e1c89cc6af54aa326b6816b0e738ee4fba/src/Optionary/OptionaryAttribute.cs)

How the configuration keys are spelled: those of every bound property and constructor parameter of the options type and of the object types nested in it, and the default section path derived from the type name. The default is [ConfigurationKeyStyle.Undefined](/Optionary/api/optionary/configurationkeystyle/fields.md), which keeps the declared names. See [ConfigurationKeyStyle](/Optionary/api/optionary/configurationkeystyle.md).

```csharp
public ConfigurationKeyStyle KeyStyle { get; set; }
```

<a id="registeroptions"></a>

## RegisterOptions

- Name: `RegisterOptions`
- Modifiers: `get; set`
- Returns: `bool`
- Source: [GitHub](https://github.com/RocketSurgeonsGuild/Optionary/blob/Branch.main.Sha.3de159e1c89cc6af54aa326b6816b0e738ee4fba/src/Optionary/OptionaryAttribute.cs)

Whether to register `IOptions<T>` for the type. `false` only describes the section in the schema. The default is `true`.

```csharp
public bool RegisterOptions { get; set; }
```

<a id="section"></a>

## Section

- Name: `Section`
- Modifiers: `nullable` `get`
- Returns: `string?`
- Source: [GitHub](https://github.com/RocketSurgeonsGuild/Optionary/blob/Branch.main.Sha.3de159e1c89cc6af54aa326b6816b0e738ee4fba/src/Optionary/OptionaryAttribute.cs#L50)

The section path, or `null` for the one derived from the type name.

```csharp
public string? Section { get; }
```

<a id="validateonstart"></a>

## ValidateOnStart

- Name: `ValidateOnStart`
- Modifiers: `get; set`
- Returns: `bool`
- Source: [GitHub](https://github.com/RocketSurgeonsGuild/Optionary/blob/Branch.main.Sha.3de159e1c89cc6af54aa326b6816b0e738ee4fba/src/Optionary/OptionaryAttribute.cs)

Whether to validate the options when the host starts. The default is `false`.

```csharp
public bool ValidateOnStart { get; set; }
```

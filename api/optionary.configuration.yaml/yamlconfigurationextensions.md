# YamlConfigurationExtensions

- Kind: `class`
- Package: [Optionary.Configuration.Yaml](/Optionary/api/optionary.configuration.yaml.md)
- Version: `0.0.1-beta.8`
- Namespace: `Optionary.Configuration.Yaml`
- Target framework: `net10.0`

Extension methods for adding YAML configuration sources to an `Configuration.IConfigurationBuilder`.

## Definition

```csharp
namespace Optionary.Configuration.Yaml;

public static class YamlConfigurationExtensions
{
    // ...
}
```

## Methods

- [AddYamlFile(IConfigurationBuilder, string)](/Optionary/api/optionary.configuration.yaml/yamlconfigurationextensions/methods.md#addyamlfile-iconfigurationbuilder-string) : `IConfigurationBuilder` `extension` -- Adds the YAML configuration provider at `path` to `builder`.
- [AddYamlFile(IConfigurationBuilder, string, bool)](/Optionary/api/optionary.configuration.yaml/yamlconfigurationextensions/methods.md#addyamlfile-iconfigurationbuilder-string-bool) : `IConfigurationBuilder` `extension` -- Adds the YAML configuration provider at `path` to `builder`.
- [AddYamlFile(IConfigurationBuilder, string, bool, bool)](/Optionary/api/optionary.configuration.yaml/yamlconfigurationextensions/methods.md#addyamlfile-iconfigurationbuilder-string-bool-bool) : `IConfigurationBuilder` `extension` -- Adds the YAML configuration provider at `path` to `builder`.
- [AddYamlFile(IConfigurationBuilder, IFileProvider?, string, bool, bool)](/Optionary/api/optionary.configuration.yaml/yamlconfigurationextensions/methods.md#addyamlfile-iconfigurationbuilder-ifileprovider-string-bool-bool) : `IConfigurationBuilder` `extension` -- Adds a YAML configuration source to `builder`.
- [AddYamlFile(IConfigurationBuilder, Action<YamlConfigurationSource>)](/Optionary/api/optionary.configuration.yaml/yamlconfigurationextensions/methods.md#addyamlfile-iconfigurationbuilder-action-yamlconfigurationsource) : `IConfigurationBuilder` `extension` -- Adds a YAML configuration source to `builder`.
- [AddYamlStream(IConfigurationBuilder, Stream)](/Optionary/api/optionary.configuration.yaml/yamlconfigurationextensions/methods.md#addyamlstream-iconfigurationbuilder-stream) : `IConfigurationBuilder` `extension` -- Adds a YAML configuration source to `builder` that reads from a `IO.Stream`.

# YamlConfigurationSource

- Kind: `class`
- Package: [Optionary.Configuration.Yaml](/Optionary/pr-preview/pr-16/api/optionary.configuration.yaml.md)
- Version: `0.0.1-pr.10`
- Namespace: `Optionary.Configuration.Yaml`
- Target framework: `net10.0`
- Inherits: `FileConfigurationSource`

A YAML file based `Configuration.FileConfigurationSource`.

## Definition

```csharp
namespace Optionary.Configuration.Yaml;

public class YamlConfigurationSource
    : Microsoft.Extensions.Configuration.FileConfigurationSource
{
    // ...
}
```

## Constructors

- [YamlConfigurationSource](/Optionary/pr-preview/pr-16/api/optionary.configuration.yaml/yamlconfigurationsource/constructors.md#constructor)

## Methods

- [Build(IConfigurationBuilder)](/Optionary/pr-preview/pr-16/api/optionary.configuration.yaml/yamlconfigurationsource/methods.md#build-iconfigurationbuilder) : `IConfigurationProvider` -- Builds the `Configuration.IConfigurationProvider` for this source.

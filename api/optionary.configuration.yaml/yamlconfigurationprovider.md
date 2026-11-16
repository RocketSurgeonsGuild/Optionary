# YamlConfigurationProvider

- Kind: `class`
- Package: [Optionary.Configuration.Yaml](/Optionary/api/optionary.configuration.yaml.md)
- Version: `0.0.1-beta.12`
- Namespace: `Optionary.Configuration.Yaml`
- Target framework: `net10.0`
- Inherits: `FileConfigurationProvider`

A YAML file based `Configuration.FileConfigurationProvider`.

## Definition

```csharp
namespace Optionary.Configuration.Yaml;

public class YamlConfigurationProvider
    : Microsoft.Extensions.Configuration.FileConfigurationProvider
{
    // ...
}
```

## Remarks

The yaml configuration provider

## Constructors

- [YamlConfigurationProvider(YamlConfigurationSource)](/Optionary/api/optionary.configuration.yaml/yamlconfigurationprovider/constructors.md#constructor-yamlconfigurationsource) -- A YAML file based `Configuration.FileConfigurationProvider`.

## Methods

- [Load(Stream)](/Optionary/api/optionary.configuration.yaml/yamlconfigurationprovider/methods.md#load-stream) -- Loads this provider's data from a stream.

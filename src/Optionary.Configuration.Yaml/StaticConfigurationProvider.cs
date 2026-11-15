using Microsoft.Extensions.Configuration;

namespace Optionary.Configuration.Yaml;

internal class StaticConfigurationProvider : ConfigurationProvider
{
    public StaticConfigurationProvider(IDictionary<string, string?> data) => Data = data ?? throw new ArgumentNullException(nameof(data));
}

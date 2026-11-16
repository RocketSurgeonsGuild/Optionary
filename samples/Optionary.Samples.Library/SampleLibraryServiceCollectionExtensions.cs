using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Optionary.Samples.Library;

/// <summary>Registers the sample library's services and options.</summary>
public static class SampleLibraryServiceCollectionExtensions
{
    /// <summary>Adds the <see cref="Greeter" /> and the options it reads.</summary>
    public static IServiceCollection AddSampleLibrary(this IServiceCollection services)
    {
        // Generated into this assembly, internal, in its RootNamespace: it registers every declaration above and the
        // [OptionsValidator]. Calling it from a public method keeps it out of the library's API.
        services.AddOptionaryConfigurationOptions();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<Greeter>();
        return services;
    }
}

namespace Optionary.Runtime.Tests;

/// <summary>
///     A placeholder so the test pipeline has something to run until the runtime tests arrive with the code import
///     (RocketSurgeonsGuild/Optionary#4). Delete it then.
/// </summary>
public sealed class ScaffoldTests
{
    [Test]
    [Arguments("Optionary")]
    [Arguments("Optionary.Configuration.Yaml")]
    public async Task Package_assemblies_load(string assemblyName)
    {
        var assembly = System.Reflection.Assembly.Load(new System.Reflection.AssemblyName(assemblyName));

        await Assert.That(assembly.GetName().Name).IsEqualTo(assemblyName);
    }
}

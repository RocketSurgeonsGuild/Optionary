using System.Runtime.CompilerServices;
using DiffEngine;
using Microsoft.Build.Utilities.ProjectCreation;

// The tests run in parallel: each one scaffolds its own temp folder and builds it out of process. The projects they create in-proc
// share MSBuild.ProjectCreation's global ProjectRootElementCache, so a project must be created without a path
// (ProjectCreator.Create(), ProjectCreator.Templates.SdkCsproj()) and get its real path from OptionaryTestProject.AddProject: two
// tests creating the same relative path share a cache entry, and renaming it on save throws in the other test.
// Every test runs one `dotnet` build at a time, which uses several cores and a lot of memory itself: a quarter of the cores (at
// least two) keeps a developer machine responsive and still overlaps the builds' idle time on a CI runner.
[assembly: ParallelLimiter<Optionary.Build.Tests.AQuarterOfTheProcessors>]

namespace Optionary.Build.Tests;

public sealed class AQuarterOfTheProcessors : TUnit.Core.Interfaces.IParallelLimit
{
    public int Limit => Math.Max(2, Environment.ProcessorCount / 4);
}

internal static class Config
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Before anything touches Microsoft.Build: the test project compiles against the packages but loads MSBuild from the SDK.
        MSBuildAssemblyResolver.Register();

        DiffRunner.Disabled = true;
        DerivePathInfo((sourceFile, _, type, method) => new(Path.Combine(Path.GetDirectoryName(sourceFile)!, "snapshots"), type.Name, method.Name));
    }
}

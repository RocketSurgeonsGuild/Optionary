# Plan: extract Dovetail's configuration schema and binding into Optionary

Status: **draft for review** (2026-11-15). Nothing has been moved or scaffolded. The decisions below were settled in the design
grilling. Source facts were checked against Dovetail `main` at `e35038ab3c`.

> **Scope change (2026-11-16).** Optionary is only the options binder and the per-application JSON Schema. Configuration-format
> support (JSON, TOML, YAML) and every Dovetail joint stay in Dovetail, so Optionary knows nothing about Dovetail. D12 is
> withdrawn, D2 holds without exception, and section 4.6 now records that format support stays in Dovetail.
> `Optionary.Configuration.Yaml` was imported in Phase 1 and removed again by RocketSurgeonsGuild/Optionary#34; its
> `0.1.0-beta.1` package is to be unlisted on nuget.org. Sections 3.1, 5, 6, 8 and 9 are updated to match.

- Library: **Optionary**, at github.com/RocketSurgeonsGuild/Optionary.
- Package: `Optionary`.
- Vocabulary: [GLOSSARY.md](../GLOSSARY.md).

## 1. Decisions

| #   | Area                 | Decision                                                                                                                                                                                                                                                                                                                    |
| --- | -------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| D1  | Repository           | A separate repository, not a package inside Dovetail.                                                                                                                                                                                                                                                                       |
| D2  | Independence         | Optionary knows nothing about Dovetail: no reference, no detection, no joint. Configuration-format packages and joints stay in Dovetail (4.6).                                                                                                                                                                              |
| D3  | Registration method  | Optionary's generator emits a plain `IServiceCollection` registration method. It is emitted whenever the attribute type is in the compilation, even if every declaration was rejected (contract in section 4).                                                                                                              |
| D4  | Dovetail's side      | Dovetail's generator detects Optionary's attribute by metadata name (strings only). It emits a joint, nested in the exports class and listed first, whose body calls the registration method.                                                                                                                               |
| D5  | Contract             | The attribute names and the method's name and signature are a cross-repo contract, treated as public API.                                                                                                                                                                                                                   |
| D6  | Renames              | **Clean break.** No shims for `[DovetailConfiguration]` or the `Dovetail*` properties. Dovetail's release notes list every rename.                                                                                                                                                                                          |
| D7  | Old fragments        | Optionary reads only the `Optionary.Schema.v1:` prefix. Libraries packed with Dovetail 0.4/0.5 stop contributing to merged schemas until they are rebuilt.                                                                                                                                                                  |
| D8  | Package IDs          | `Optionary` (runtime, generator, MSBuild tasks, targets), the only package. Root namespace `Optionary`.                                                                                                                                                                                                                     |
| D9  | Attribute            | `[Optionary]` / `[Optionary("Section")]` on a class, and `[assembly: Optionary<T>("Section")]`.                                                                                                                                                                                                                             |
| D10 | Other names          | Short `OptionarySchema*` MSBuild properties, the `Optionary.Schema.v1:` prefix, generated files `Optionary.Schema.g.cs` and `Optionary.Options.g.cs`, and diagnostics renumbered from `OPT0001` (section 3).                                                                                                                |
| D11 | Contract shape       | `internal static class OptionaryConfigurationOptions` in the project's `RootNamespace`, with `internal static IServiceCollection AddOptionaryConfigurationOptions(this IServiceCollection services)`. It is emitted whenever the attribute type resolves. If the generator doesn't run, the build fails with a clear error. |
| D12 | YAML                 | **Withdrawn (2026-11-16).** It made `Optionary.Configuration.Yaml` a package that declared Dovetail's YAML joints as templates. Format support stays in Dovetail instead (4.6), and `Joinery.Dovetail.Configuration.Yaml` stays.                                                                                            |
| D13 | Template repo        | **Dovetail, trimmed** (section 5).                                                                                                                                                                                                                                                                                          |
| D14 | Prereleases          | nuget.org prerelease tags (`v0.1.0-beta.N`) through the existing tag-publish workflow. No new feed.                                                                                                                                                                                                                         |
| D15 | Dovetail dependency  | **Opt-in.** `Joinery.Dovetail` does not depend on Optionary; users add it. Dovetail's targets set Optionary's hook properties, which have no effect when Optionary is absent. There is no SDK switch for now.                                                                                                               |
| D16 | ZeroAlloc.Validation | The `ValidateWithZeroAlloc()` integration moves to Optionary, controlled by `OptionaryZeroAllocValidation`.                                                                                                                                                                                                                 |
| D17 | Public API tracking  | Optionary actually tracks its public API: `Microsoft.CodeAnalysis.PublicApiAnalyzers`, `PublicAPI.Shipped/Unshipped.txt` per packable project, RS0016 and RS0017 as errors.                                                                                                                                                 |
| D18 | Target frameworks    | Unchanged. Runtime: `net10.0;net11.0`. Generator: `netstandard2.0` in roslyn5.0/5.3/5.6/5.9 slices. Tasks: `netstandard2.0`, compiled against STJ/SRM 9.0.0.                                                                                                                                                                |
| D19 | History              | **Snapshot only.** One import commit that records the Dovetail source commit and a path map (section 7).                                                                                                                                                                                                                    |
| D20 | Real-build tests     | Split. The generic schema-target cases move to a new `test/Optionary.Build.Tests`. Dovetail's `Sdk.Tests` keep the Dovetail integration cases (section 8).                                                                                                                                                                  |

## 2. Verified source, and corrections to the brief

The brief's tables hold, with these additions and corrections.

Things that move but weren't listed:

- **ZeroAlloc.Validation integration**
    - Moves: `Support/Configuration/ZeroAllocOptionsValidation.cs`, DT0018, and the `DovetailEnableZeroAllocValidation` / `ZeroAllocGeneratedAccessibility` reads in `ConfigurationSchemaPipeline.cs:112-117`.
    - Stays in Dovetail: `Joinery.Dovetail.ZeroAlloc.targets` and its joints.
- **Reference documentation target.** `_DovetailConfigurationReferenceDocumentation` (`Joinery.Dovetail.targets:95-108`) passes package XML docs as `AdditionalFiles`. It is controlled by `DovetailConfigurationReferenceDocumentation`.
- **Compiler-visible schema properties**, in `Joinery.Dovetail.targets:45-49`: `CompilerVisibleProperty` for `DovetailConfigurationSchemaRequired` and `DovetailEnableZeroAllocValidation`, and `CompilerVisibleItemMetadata AdditionalFiles DovetailReferenceDocumentation`.
- **Shared generator helpers** (copy them; they also stay in Dovetail): `Helpers.AddAssemblyAttribute`, `Helpers.CompilerGeneratedAttributes`, `AddSharedTrivia`, `GetBuildProperty`, `IsOpenGenericType`. Also the configuration descriptors in the shared `Diagnostics.cs`.
- **SchemaStore copy.** `Build.Tasks/SchemaStore/appsettings.json` (1,031 lines, Apache-2.0) and `THIRD-PARTY-NOTICES.txt`, with the `UpdateSchemaStoreAppSettings` target.

Things that split:

- **DT0006** (C# 12 guard) is shared with the joints. Dovetail keeps DT0006, and Optionary gets its own (`OPT0001`).
- **`ConfigurationOptionsEmitter.cs`** emits both the joint (Dovetail) and the binder and registrations (Optionary).
- **`ExportDovetails.cs:16-40`** takes `hasConfigurationOptions` from the pipeline's return value. That value becomes a Dovetail-side detection.

The schema targets couple to Dovetail in two places, which become hook properties (section 4.5):

- `_DovetailApplicationDetected`, from `Joinery.Dovetail.Sdk`'s `HostDetection.targets`.
- `DovetailEmbedConfiguration`, which marks MAUI/Uno hosts that don't copy the schema.

Corrections to the YAML row (moot since the scope change: the YAML package stays in Dovetail, 4.6):

- The three joints live **inside** `Joinery.Dovetail.Configuration.Yaml`.
- Its `build/…Yaml.targets` adds `BlazorConfigurationFile` metadata for the browser joint.
- `Joinery.Dovetail/build/DovetailY{a}mlConfiguration.cs.template` contains `using Joinery.Dovetail.Configuration.Yaml;`.
- `Joinery.Dovetail.PackConfiguration.targets:305` detects the provider by the reference name `Joinery.Dovetail.Configuration.Yaml`.
- `Joinery.Dovetail.Sdk/Sdk/Joinery.Dovetail.Configuration.targets:17` references it implicitly.
- The joints use only public API (`AddYamlFile`, `AddYamlStream`), so the four `InternalsVisibleTo` entries are dropped.

Corrections to the design docs and the brief's conventions:

- `OptionaryAttribute<T>` keeps `AllowMultiple = true`. The P1 plan says `false`/CS0579, but the code is `true` and reports a repeated type as DT0015. Optionary keeps what the code does.
- `RS0017` as an error does nothing in Dovetail and Specular today: neither references the PublicAPI analyzers or has `PublicAPI.*.txt` files. D17 fixes this for Optionary only.

Other files that use the moving feature's names and stay in Dovetail:

- `Joinery.Dovetail.Serilog/SerilogConfigurationSchema.cs:28` hard-codes the key `Dovetail.ConfigurationSchema.v1:Serilog`.
- The schema endpoint (`Joinery.Dovetail.Hosting`) depends only on the file name `appsettings.schema.json` under `$(BaseIntermediateOutputPath)`. It already answers 404 when the file is missing, so it keeps working under opt-in.
- `Joinery.Dovetail.PackConfiguration.targets:73` excludes `appsettings.schema.json`.

Size: about 7,500 lines move.

| Part      | Lines                            |
| --------- | -------------------------------- |
| Generator | 4,030 (binder port 2,570)        |
| Tasks     | 2,830, plus the SchemaStore JSON |
| Targets   | 307                              |
| Runtime   | 336                              |

Test cross-dependencies inside the four "moving" test projects are untangled in Phase 0 (section 6):

- `Build.Tasks.Tests/SerilogSchemaTests` uses `typeof(SerilogJoint)`.
- `Configuration.Runtime.Tests/EmbeddedConfigurationTests` uses joints, `DovetailContextBuilder` and the JSON template.
- `Configuration.Aot.Tests/GeneratedConstructorParameterTests` is a joint test.
- The Aot and Parity tests bind through `Rivets.Exports.Joints()`.

## 3. Package layout and rename map

### 3.1 Projects and packages

One package, `Optionary`.

| Project                                                                                      | TFM             | Packable          | Ships in                                                                                                                                                                                                                                 |
| -------------------------------------------------------------------------------------------- | --------------- | ----------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `src/Optionary`                                                                              | net10.0;net11.0 | yes (`Optionary`) | `lib/`. It also packs the generator to `analyzers/dotnet/roslyn5.{0,3,6,9}/cs/Optionary.Analyzers.dll`, the tasks to `build/tasks/netstandard2.0/Optionary.Build.Tasks.dll`, and `build/`, `buildTransitive/` and `buildMultiTargeting/` |
| `src/Optionary.Analyzers` (+ `src/Optionary.Analyzers.supports/` roslyn5.0/5.3/5.6 projects) | netstandard2.0  | no                | inside `Optionary`                                                                                                                                                                                                                       |
| `src/Optionary.Build.Tasks`                                                                  | netstandard2.0  | no                | inside `Optionary`                                                                                                                                                                                                                       |

What each package contains:

- `Optionary` runtime:
    - `OptionaryAttribute` and `OptionaryAttribute<T>`;
    - `ConfigurationKeyStyle`;
    - `OptionaryServiceCollectionExtensions.AddOptionaryOptions<TOptions>(services, sectionPath, create)`;
    - the internal `Infrastructure.OptionaryOptionsFactory<T>`.
- `Optionary` build assets:
    - `Optionary.props` and `Optionary.targets`, which carry the compiler-visible properties, the reference-docs target and the generator check;
    - `Optionary.Schema.targets`, `Optionary.Schema.VisualStudio.targets` and `Optionary.Schema.EditorSettings.targets`.

    The build assets flow transitively, as Dovetail's do today: a host that references a library built with Optionary merges that library's fragments.

There is no configuration-format package: Optionary binds from whatever `IConfiguration` holds, and the providers (JSON,
TOML, YAML) and their joints stay in Dovetail (4.6).

### 3.2 Rename map

**Types and namespaces**

| Dovetail                                                                                     | Optionary                                                               |
| -------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------- |
| `Joinery.Dovetail.Attributes.DovetailConfigurationAttribute`                                 | `Optionary.OptionaryAttribute`                                          |
| `Joinery.Dovetail.Attributes.DovetailConfigurationAttribute<T>`                              | `Optionary.OptionaryAttribute<T>`                                       |
| `Joinery.Dovetail.Attributes.ConfigurationKeyStyle` (values 0–4 unchanged)                   | `Optionary.ConfigurationKeyStyle`                                       |
| `Joinery.Dovetail.DovetailConfigurationOptionsExtensions.AddDovetailConfigurationOptions<T>` | `Optionary.OptionaryServiceCollectionExtensions.AddOptionaryOptions<T>` |
| `Joinery.Dovetail.Infrastructure.DovetailOptionsFactory<T>` (internal)                       | `Optionary.Infrastructure.OptionaryOptionsFactory<T>`                   |
| `Joinery.Dovetail.Support.Configuration[.Binding]` (internal)                                | `Optionary.Analyzers[.Binding]`                                         |
| `Joinery.Dovetail.Build.Tasks.*` (task class names unchanged)                                | `Optionary.Build.Tasks.*`                                               |

**Generated code**

| Dovetail                                                                         | Optionary                                                                                                                                      |
| -------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| `Dovetail_ConfigurationSchema.g.cs`                                              | `Optionary.Schema.g.cs`                                                                                                                        |
| `Dovetail_ConfigurationOptions.g.cs`                                             | `Optionary.Options.g.cs`                                                                                                                       |
| Private joint `DovetailConfigurationOptions`, nested in `Exports`                | Gone from Optionary. In its place, `OptionaryConfigurationOptions.AddOptionaryConfigurationOptions` (section 4). Dovetail emits its own joint. |
| `file static class DovetailConfigurationBinder`                                  | `file static class OptionaryBinder`                                                                                                            |
| Tracking names `dovetail:configuration_*`                                        | `optionary:*`                                                                                                                                  |
| Assembly-metadata prefix `Dovetail.ConfigurationSchema.v1:`                      | `Optionary.Schema.v1:` (the task's version check reads `Optionary.Schema.` + `v1:`)                                                            |
| Generated document text "Generated by Dovetail…" and task messages "Dovetail: …" | "Optionary …"                                                                                                                                  |

**Public MSBuild properties and metadata**

| Dovetail                                                                   | Optionary                                                    |
| -------------------------------------------------------------------------- | ------------------------------------------------------------ |
| `DovetailConfigurationSchema`                                              | `OptionarySchema`                                            |
| `DovetailConfigurationSchemaHost`                                          | `OptionarySchemaHost`                                        |
| `DovetailConfigurationSchemaStrict`                                        | `OptionarySchemaStrict`                                      |
| `DovetailConfigurationSchemaIncludeSchemaStore`                            | `OptionarySchemaIncludeSchemaStore`                          |
| `DovetailConfigurationSchemaRequired`                                      | `OptionarySchemaRequired`                                    |
| `DovetailConfigurationSchemaVisualStudio`                                  | `OptionarySchemaVisualStudio`                                |
| `DovetailScaffoldEditorSettings`                                           | `OptionaryScaffoldEditorSettings`                            |
| `DovetailEditorSettingsDirectory`                                          | `OptionaryEditorSettingsDirectory`                           |
| `DovetailConfigurationReferenceDocumentation`                              | `OptionaryReferenceDocumentation`                            |
| `AdditionalFiles` metadata `DovetailReferenceDocumentation`                | `OptionaryReferenceDocumentation`                            |
| `DovetailEnableZeroAllocValidation`                                        | `OptionaryZeroAllocValidation`                               |
| (internal: `DovetailEmbedConfiguration` checked inside the schema targets) | New `OptionarySchemaCopyToOutput` (default `true` for hosts) |

**Private MSBuild names.** `_DovetailConfigurationSchema*` targets, properties and items become `_OptionarySchema*`. `_DovetailBuildTasksAssembly` becomes `_OptionaryBuildTasksAssembly`. `$(MSBuildProjectName).ConfigurationSchema.{inputs,stamp}` becomes `.OptionarySchema.{inputs,stamp}`.

**What doesn't change.** File names stay `appsettings.schema.json` (host) and `ConfigurationSchema.json` (library, nupkg root, Aspire-compatible), with the VS `FilePathPattern` `appsettings\..*json`. VS Code entries are matched by schema path, so existing `.vscode/settings.json` mappings stay valid.

**Diagnostics.** IDs are renumbered and the category becomes `Optionary`.

| Dovetail                       | Optionary       | Rule                                                                                                                                          |
| ------------------------------ | --------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| DT0006 (stays in Dovetail too) | OPT0001         | C# older than 12: the registration method is emitted with an empty body, nothing else                                                         |
| DT0007                         | OPT0002         | invalid options type                                                                                                                          |
| DT0008                         | OPT0003         | invalid section path                                                                                                                          |
| DT0009                         | OPT0004         | unsupported member                                                                                                                            |
| DT0010                         | OPT0005         | key collision                                                                                                                                 |
| DT0011                         | OPT0006         | type can't be created                                                                                                                         |
| DT0012                         | OPT0007         | type graph too large                                                                                                                          |
| DT0013                         | OPT0008         | annotation not expressible                                                                                                                    |
| DT0014                         | OPT0009         | schema generation failed                                                                                                                      |
| DT0015                         | OPT0010         | type declared twice                                                                                                                           |
| DT0016                         | OPT0011         | key style conflict                                                                                                                            |
| DT0018                         | OPT0012         | ZeroAlloc async rule                                                                                                                          |
| new                            | OPT1001         | MSBuild error from `Optionary.targets`: the runtime is referenced but the generator isn't loaded                                              |
| new                            | OPT1002–OPT1005 | MSBuild errors for `OptionarySchemaFragment` items: not exactly one of `Schema`/`File`, an invalid section path, no such file, not a fragment |

All of them start in Optionary's `AnalyzerReleases.Unshipped.md`. Dovetail moves DT0007–DT0016 and DT0018 to "Removed Rules".

## 4. The cross-repo contract (D3–D5, D11)

Both sides can be built from this section alone. "MUST" items are covered by tests in both repos (section 8).

### 4.1 Attribute types (Optionary, runtime assembly named exactly `Optionary`)

- `Optionary.OptionaryAttribute`:
    - metadata name `Optionary.OptionaryAttribute`;
    - `[AttributeUsage(AttributeTargets.Class)]`, `[Conditional("CodeGeneration")]`, sealed;
    - constructors `()` and `(string section)`;
    - properties `Section`, `RegisterOptions` (default `true`), `ValidateOnStart` and `KeyStyle`.
- `Optionary.OptionaryAttribute<T> where T : class`:
    - metadata name ``Optionary.OptionaryAttribute`1``;
    - `[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]`, `[Conditional("CodeGeneration")]`;
    - the same members.
- `Optionary.ConfigurationKeyStyle` has `Undefined = 0, Pascal = 1, Camel = 2, Dash = 3, Underscore = 4`.

Because of `[Conditional("CodeGeneration")]` the attributes never reach metadata. All detection is therefore in-compilation, from syntax.

### 4.2 The registration method (Optionary MUST)

- **When:** in every compilation where `GetTypeByMetadataName("Optionary.OptionaryAttribute")` resolves to a type whose `ContainingAssembly.Name` is `Optionary`. This covers every case Dovetail can detect:
    - no declarations at all;
    - every declaration rejected;
    - every declaration `RegisterOptions = false`;
    - the C# version is below 12 (empty body, plus OPT0001).
- **Where:** hint name `Optionary.Options.g.cs`. The namespace is `build_property.RootNamespace` **verbatim**; `Optionary.props` adds `CompilerVisibleProperty RootNamespace`. When it is missing, empty or whitespace, the class goes in the global namespace.
- **Shape:**

    ```csharp
    namespace <RootNamespace>;

    [global::System.CodeDom.Compiler.GeneratedCode("Optionary", "<version>")]
    internal static class OptionaryConfigurationOptions
    {
        internal static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddOptionaryConfigurationOptions(
            this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)
        {
            // one AddOptionaryOptions<T>(services, "<path>", static configuration => OptionaryBinder.Create…(configuration))
            // per registered type, in path (ignoring case, then ordinal) then type-name order,
            // wrapped in ValidateWithZeroAlloc()/ValidateOnStart() as today; then TryAddEnumerable for [OptionsValidator] classes
            return services;
        }
    }
    ```

- **Repeat calls are harmless.** Every registration is `TryAdd`/`TryAddEnumerable`, so a library author's own `AddMyLibrary()` and a Dovetail joint can both call it.
- **Internal on purpose.** It never becomes part of a library's public API (D17). Library authors call it from their own public extension method.

### 4.3 The joint (Dovetail MUST)

- **Detection.** `ForAttributeWithMetadataName("Optionary.OptionaryAttribute")` on class and record declarations, and ``("Optionary.OptionaryAttribute`1")`` on the compilation unit. A match counts only when the attribute class's `ContainingAssembly.Name == "Optionary"`. At least one match means `hasOptionaryDeclarations`.
- **Emission.** Only when `hasOptionaryDeclarations` is true and Dovetail's own C# 12 guard passes (otherwise DT0006, nothing emitted).
    - A private joint class nested in the assembly's exports class: `Register(context, services)` calls `global::<RootNamespace>.OptionaryConfigurationOptions.AddOptionaryConfigurationOptions(services);`, or `global::OptionaryConfigurationOptions…` when `RootNamespace` is empty.
    - The joint's class name is Dovetail's choice and not part of the contract.
    - The namespace comes from `build_property.RootNamespace`, **not** from `DovetailExportNamespace`.
- **Ordering.** The joint is yielded first in the exports list, with `DovetailHostType.Undefined` and the Application category, exactly as `ExportDovetails.cs:20-40` does today.

### 4.4 Failure modes

| Situation                                                                                                                                                                     | Result                                                                                                                                                                                                                                                                                                                                                                                                                                                               |
| ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Optionary not referenced                                                                                                                                                      | No attribute type: no method, no joint.                                                                                                                                                                                                                                                                                                                                                                                                                              |
| Attribute used, but Optionary's generator not in `@(Analyzer)` (a target removes `Optionary.Analyzers`, or a compiler older than Roslyn 5.0, which no analyzer slice matches) | `Optionary.targets` target `_OptionaryCheckGenerator` (`BeforeTargets="CoreCompile"`) fails with **OPT1001**. It fires when `Optionary.dll` is in `@(ReferencePath)` and no `@(Analyzer)` has `Filename == 'Optionary.Analyzers'`, and the message explains how to fix it.                                                                                                                                                                                           |
| `ExcludeAssets="analyzers"` on the package reference                                                                                                                          | Nothing happens: the .NET SDK (10.0.401, 11.0.100) still passes the package's analyzers to the compiler, so the generator runs and OPT1001 has nothing to report. Pinned by `GeneratorCheckTests.ExcludingTheAnalyzersAsset_StillLoadsTheGenerator`.                                                                                                                                                                                                                 |
| Optionary's build assets excluded (`ExcludeAssets="build"` or `"all"`)                                                                                                        | `Optionary.props`/`.targets` aren't imported, so `_OptionaryCheckGenerator` doesn't run and `CompilerVisibleProperty RootNamespace` is missing. The generator still runs but emits `OptionaryConfigurationOptions` in the global namespace, and no schema is written. The joint's `global::<RootNamespace>.OptionaryConfigurationOptions` call in Dovetail's generated `Exported_Dovetails.g.cs` fails with CS0234. Both repos' troubleshooting pages document this. |
| A user-defined `Optionary.OptionaryAttribute` in another assembly                                                                                                             | Dovetail ignores it (assembly-name guard), and Optionary emits nothing.                                                                                                                                                                                                                                                                                                                                                                                              |
| C# < 12                                                                                                                                                                       | Dovetail: DT0006, no joint. Optionary: empty method plus OPT0001. Both compile.                                                                                                                                                                                                                                                                                                                                                                                      |
| Generator order                                                                                                                                                               | Neither generator reads the other's output, so their order doesn't matter.                                                                                                                                                                                                                                                                                                                                                                                           |
| Two assemblies with the same `RootNamespace`, one with `InternalsVisibleTo` the other                                                                                         | CS0436 warning in the consumer, and the source definition wins. Documented; set distinct `RootNamespace`s.                                                                                                                                                                                                                                                                                                                                                           |

### 4.5 MSBuild-side contract (Optionary's public MSBuild API that Dovetail relies on)

These names are public API in Optionary's docs and change only in a major version.

- **Files.** `$(BaseIntermediateOutputPath)appsettings.schema.json` is the host schema (Dovetail's schema endpoint serves it from `bin/`, falling back to `obj/`). `ConfigurationSchema.json` sits at the nupkg root.
- **Fragments.** The key format is `[assembly: AssemblyMetadata("Optionary.Schema.v1:<Section path>", "<schema>")]`. Hand-written fragments such as Joinery.Dovetail.Serilog's are supported.
- **Hooks Dovetail sets.** Dovetail sets them unconditionally in `Joinery.Dovetail.targets`; they have no effect without Optionary. Optionary reads them inside `_OptionaryComputeSchema`, so values from evaluation anywhere count.
    - `OptionarySchemaHost`: Dovetail sets `true` when `_DovetailApplicationDetected` is `true` and the user left it empty.
    - `OptionarySchemaCopyToOutput`: Dovetail sets `false` when `DovetailEmbedConfiguration == true`.
    - `OptionaryZeroAllocValidation`: Dovetail sets `false` when `DovetailEnableZeroAllocValidation == false`.

### 4.6 Configuration formats stay in Dovetail

D12 is withdrawn (2026-11-16), so nothing here crosses the repo boundary:

- **Dovetail keeps its format packages.** `Joinery.Dovetail.Configuration.Yaml` (with its YAML joints and their
  `<DovetailTemplate>` items from Phase 0), the JSON and TOML support, the SDK's implicit YAML reference,
  `PackConfiguration.targets:305` and `DovetailY{a}mlConfiguration.cs.template` are unchanged.
- **Optionary ships no provider.** The binder reads `IConfiguration`, whichever providers filled it. Optionary's own tests use
  `Microsoft.Extensions.Configuration.Json`.
- **The schema still covers every format.** The application schema describes keys, not files, so the editor mappings for
  `appsettings*.yaml` and TOML files (`Optionary.Schema.EditorSettings.targets`) stay in Optionary.
- **Why.** The amendment made Optionary's package compile against Dovetail's joint API (`IConfigurationJoint`,
  `IDovetailContext`), a cross-repo coupling the section 4 contract otherwise avoids.

## 5. Repository structure (template: Dovetail, trimmed)

**Copied as-is**

- `.editorconfig`, `LICENSE`, `GitVersion.yml` (GitHubFlow/v1, main label `beta`), `GitReleaseManager.yaml` and `NuGet.config` (nuget.org only, source mapping `*`).
- `Directory.Packages.MSBuildTasks.props` (STJ/SRM pinned for `netstandard2.0`) and its `.github/renovate.json` rule.
- `.config/mise.ci.toml` and `biome.json`.
- The workflows `lint.yml`, `publish-results.yml`, `draft-release.yml`, `update-milestone.yml`, `close-milestone.yml`, `sync-labels.yml`, `dependabot-merge.yml` and `docs-preview-cleanup.yml`.
- `apm.yml`, `Skillfile` and `LEAN-CTX.md`.

**Adapted**

- **Solution.** A new `Optionary.slnx` without stale file entries, listing `src/Optionary.Analyzers.supports/` and `test/Optionary.Analyzers.Tests.supports/` (Dovetail leaves its test `.supports` projects out).
- **`global.json`.** SDK `11.0.100` GA rather than Dovetail's `11.0.100-rc.3…`, with the MTP test runner.
- **Root `Directory.Build.props/targets`.**
    - Keep: LangVersion preview, strict, nullable, `AnalysisMode`, NuGet audit, CPM, package validation and runtime-async on net11.
    - Drop: the browser-wasm asset trick.
    - Add: RS0016 to `WarningsAsErrors` next to RS0017.
- **`src/Directory.Build.props/targets`.**
    - Import Optionary's own `build/Optionary.props/targets` and `src/Optionary.Build.Tasks/Optionary.Build.Tasks.InRepo.targets` (the TaskHostFactory override, renamed).
    - Add the analyzer `ProjectReference` for `src/Optionary` only.
    - Drop Dovetail's 13-entry `InternalsVisibleTo` list for a per-project one, and drop `PrivateAssets` and the joint gating.
- **`test/Directory.Build.props/targets`.**
    - Keep: TUnit, TUnit.Mocks, `TUnit.Assertions.Should`, Verify.TUnit, `Rocket.Surgery.Extensions.Testing.TUnit` and `.SourceGenerators`, CrashDump and HangDump, and the analyzer `ProjectReference` as `OutputItemType="Analyzer"`.
    - Drop: `ImportDovetailAssembly` and the Dovetail, Hosting and InRepo-Dovetail imports.
- **`Directory.Packages.props`.**
    - Same `GlobalPackageReference` set, plus `Microsoft.CodeAnalysis.PublicApiAnalyzers`.
    - About 20 pins: Microsoft.Extensions.Configuration, Configuration.Binder, Configuration.Json (tests), Options, DependencyInjection, Hosting (tests); Microsoft.Build.Framework and Utilities.Core; Corvus.Json.JsonSchema.Draft7; MSBuild.ProjectCreation; ZeroAlloc.Validation and .Options (tests); Microsoft.CodeAnalysis.CSharp (per slice); TUnit, Verify and Rocket.Surgery testing.
- **`build/Build.cs` and `build/appsettings.yaml`.** The same file-based ModularPipelines app; change the solution name and drop `TestMode`'s runsettings if coverage settings differ. It still uses `Joinery.Dovetail.ModularPipelines`, which is build tooling only, not shipped, so it doesn't violate D2.
- **`build.yml`.** The same build, docs and `v*` tag publish (NuGet trusted publishing). Drop `MISE_ENV`-specific workload steps.
- **`.config/mise.toml`.** Drop the workload postinstall (wasm-tools, MAUI/Uno), the aspnetcore runtime and the stale `sample:sdk` task. Keep the docs tasks.
- **`.config/hk.pkl`.** Solution name; drop the sample excludes.
- **Docs (`docs/`, Astro Starlight).**
    - Copy the scaffold and rename the workspace package to `optionary-docs`.
    - Content: rewrite Dovetail's `concepts/managed-configuration.md` (499 lines) into Optionary pages: getting started, declarations, key styles, binder semantics and the deliberate differences, schema and editors, MSBuild property reference, diagnostics reference, and "registering from a library".
- **`AGENTS.md`/`CLAUDE.md`.** Regenerate through apm with an Optionary constitution. Dovetail's `AGENTS.md` embeds the stale Conventions constitution and its `CLAUDE.md` is empty.
- **Multi-Roslyn layout.** `src/Optionary.Analyzers.supports/Optionary.Analyzers.roslyn5.{0,3,6}.csproj` plus `Directory.Build.props/targets`, copied from Dovetail and renamed. Also bring over Specular's `RunAnalyzers=false` and `Private="false"` refinements.
- **Icons and README.** A new icon; the README gets an "Origins" note linking to Dovetail.

**Dropped**

- `sample/` (MAUI/Uno/Blazor/SdkSample), `benchmarks/` and `samples.yml`.
- `Joinery.Dovetail.Sdk`, every configuration-format package (4.6), and everything joint, host or template related.
- `Directory.Build.workloads.props`, `Dovetail.sln.DotSettings.user`, `.idea/`, `artifacts/`, `TestResults/`, `node_modules/`, and Dovetail's `plans/` (they stay in Dovetail as history).

## 6. Phases

Each phase leaves both repos building with tests passing. Within a PR, individual commits need not build.

### Phase 0: cut the seam inside Dovetail (Dovetail PR)

Goal: the moving code no longer touches joints, and the four moving test projects test only the moving code. Nothing is renamed yet, and users see no change except that the YAML joints become generated.

1. **Separate generator.** A new `ConfigurationGenerator : IIncrementalGenerator` in `Joinery.Dovetail.Analyzers` hosts `ConfigurationSchemaPipeline`. Its dependencies are copied into `Support/Configuration/`:
    - the helper members as `ConfigurationHelpers`;
    - the descriptors as `ConfigurationDiagnostics` (IDs unchanged);
    - its own C# version check.

    It no longer takes `MsBuildConfig` or `DovetailConfigurationData`.

2. **Registration method.** `ConfigurationOptionsEmitter` emits the contract shape from section 4.2 (`OptionaryConfigurationOptions` / `AddOptionaryConfigurationOptions` in `RootNamespace`). This phase emits it **when at least one declaration exists**, the same predicate as step 3, to avoid adding a file to every Dovetail snapshot. The type-based rule arrives with Optionary.
3. **Dovetail-owned joint.** The joint moves out of `ConfigurationOptionsEmitter` into `ExportDovetails`. `hasConfigurationOptions` comes from Dovetail's own `ForAttributeWithMetadataName` on the current `Joinery.Dovetail.Attributes.DovetailConfigurationAttribute` names, not from the pipeline's return value. The body calls the method.
4. **Targets.**
    - `Joinery.Dovetail.ConfigurationSchema.targets` reads the hook properties (`DovetailConfigurationSchemaHost`, defaulted from `_DovetailApplicationDetected` by `Joinery.Dovetail.targets`, and a new `DovetailConfigurationSchemaCopyToOutput`) instead of reaching into SDK and embed internals.
    - The reference-docs target and the schema `CompilerVisible*` entries move into the schema targets.
    - The ZeroAlloc forwarding moves into `Joinery.Dovetail.ZeroAlloc.targets`.
5. **YAML joints become templates.** The three joints become `.cs.template` files plus `<DovetailTemplate>` items in `Joinery.Dovetail.Configuration.Yaml/build/*.targets`, with the application/test gating from 4.6, and the compiled joints are deleted. The templates stay in Dovetail (4.6; D12, which this step was to prove, is withdrawn). Update `Hosting.Tests/RocketHostTests` and `Hosting.AspNetCore.Tests/RocketWebApplicationTests` (`ExceptJoint(typeof(YamlJoint))`, provider counts).
6. **Untangle the tests:**
    - `ConfigurationOptionsTests` (22 snapshots): the options-file snapshots lose `Exported_Dovetails.g.cs`. A new `OptionsJointTests` in Dovetail snapshots the joint (first in the exports, calling the method), checks for no joint without declarations or below C# 12, and covers the cached-steps checks.
    - Parity (`Binders.cs:29`) and Aot (`GeneratedJointTests`) call `services.AddOptionaryConfigurationOptions()` instead of `Rivets.Exports.Joints()`.
    - `GeneratedConstructorParameterTests` moves to a Dovetail joint test project.
    - `EmbeddedConfigurationTests` (7 tests plus the `embedded/*` fixtures) moves out of `Configuration.Runtime.Tests` into a new `test/Joinery.Dovetail.Configuration.Tests`, and the Json/Toml/Yaml references go with it.
    - `SerilogSchemaTests` (13 tests, 2 snapshots) moves out of `Build.Tasks.Tests`. Its merge semantics become a generic "hand-written fragment" test with the Serilog JSON copied as a fixture (it moves with the tasks in Phase 1). Dovetail keeps Serilog coverage through `Sdk.Tests/ConfigurationSchemaSerilogTests` (4 real builds) and a new unit test that validates `SerilogConfigurationSchema` against draft-07.

**Verification**

- Full `dotnet test` in Dovetail; review every snapshot diff (expect only the joint and options-file split).
- `rg -n 'Helpers\.|Diagnostics\.|MsBuildConfig|Joinery\.Dovetail\.Joints|IServiceJoint' src/Joinery.Dovetail.Analyzers/Support/Configuration` is empty.
- The four moving test projects reference only `Joinery.Dovetail` (the runtime pieces) and the tasks.
- `Sdk.Tests` pass unchanged, apart from the YAML joint templates.
- Record the merge SHA as the **import SHA**.

### Phase 1: scaffold and import (Optionary PR into `main`)

1. **Scaffold commit.** The section 5 infrastructure, with empty projects. CI green (build, lint, docs).
2. **Import commit:** "Import configuration schema and binding from RocketSurgeonsGuild/Dovetail@<import SHA>". A pure copy into the new paths, with the path map in the commit body (section 7).
3. **Rename commit.** Section 3.2, mechanically. Renumber the diagnostics, write `AnalyzerReleases.Unshipped.md`, and generate `PublicAPI.Unshipped.txt` with the RS0016 code fix, then review it.
4. **Contract commit:**
    - type-based emission with the assembly-name guard;
    - OPT0001 with an empty method below C# 12;
    - `CompilerVisibleProperty RootNamespace`;
    - the `_OptionaryCheckGenerator` target (OPT1001);
    - the hook properties `OptionarySchemaHost` and `OptionarySchemaCopyToOutput`.
5. **Tests** (section 8), including the new `Optionary.Build.Tests` and contract tests.
6. **Docs site content.**

**Verification**

- `dotnet test` on every test project, on net10.0 and net11.0.
- `dotnet pack`, then inspect the nupkg:
    - analyzer slices `roslyn5.{0,3,6,9}`;
    - `build/tasks/netstandard2.0/Optionary.Build.Tasks.dll`, with no STJ/SRM copies;
    - targets, `THIRD-PARTY-NOTICES.txt`, `PublicAPI` coverage and package validation.
- A scratch console app outside the repo (`dotnet new console`, local folder feed, `[Optionary] class GreetingsOptions`):
    - `IOptions<GreetingsOptions>` binds;
    - `obj/appsettings.schema.json` has a strict `Greetings` section plus SchemaStore;
    - removing the generator from `@(Analyzer)` in a target produces OPT1001 (`ExcludeAssets="analyzers"` doesn't: the SDK still loads the package's analyzers).

### Phase 2: publish a prerelease (Optionary)

1. **One-time setup:**
    - reserve or own the `Optionary` ID on nuget.org;
    - add a trusted-publishing policy for `RocketSurgeonsGuild/Optionary` / `build.yml`;
    - set the `NUGET_USER`, `RSG_BOT_TOKEN` and `CODECOV_TOKEN` secrets;
    - enable gh-pages.
2. Tag `v0.1.0-beta.1` and let `build.yml` publish.

`v0.1.0-beta.1` also published `Optionary.Configuration.Yaml`, before the scope change. A package owner unlists it.

**Verification:** the package is listed on nuget.org, a restore from a cleared cache works, and the Phase 1 scratch app works against the published version.

### Phase 3: switch Dovetail to the package (Dovetail PR)

Everything in this phase lands in one PR. While both generators or both schema targets are active, they produce the same class name (CS0101) and both write `appsettings.schema.json`.

1. **Versions.** `Directory.Packages.props` pins `Optionary` to `0.1.0-beta.1`.
2. **Generator.**
    - Detection strings change to the section 4.3 names, with the assembly guard.
    - The joint body calls `global::<RootNamespace>.OptionaryConfigurationOptions.AddOptionaryConfigurationOptions(services)`.
    - `ConfigurationGenerator` is unregistered, so Dovetail emits nothing of its own for configuration.
3. **Runtime (clean break).** Remove `DovetailConfigurationAttribute`, `DovetailConfigurationAttribute<T>`, `ConfigurationKeyStyle`, `DovetailConfigurationOptionsExtensions` and `DovetailOptionsFactory` from `Joinery.Dovetail`.
4. **Targets.**
    - `Joinery.Dovetail.targets` stops importing the schema targets and stops packing the tasks DLL.
    - It adds the section 4.5 hook settings.
    - The InRepo tasks import is removed from `src`, `test`, `sample` and `benchmarks` `Directory.Build.targets`, and the `DovetailConfigurationSchema=false` line from `src/Directory.Build.targets:7`.
5. **Formats.** Dovetail keeps its format packages (4.6): `Joinery.Dovetail.Configuration.Yaml` and its templates, the SDK's
   implicit reference, `PackConfiguration.targets:305` and `DovetailY{a}mlConfiguration.cs.template` don't change.
6. **Serilog.** `SerilogConfigurationSchema.Key` becomes `Optionary.Schema.v1:Serilog`.
7. **Sample.** `sample/SdkSample/SdkSample.Greetings` adds `<PackageReference Include="Optionary" />` and `[Optionary]`.
8. **Tests.**
    - `Sdk.Tests` integration cases reference the pinned Optionary version (section 8).
    - `OptionsJointTests` uses a stub assembly named `Optionary`.

**Verification**

- Full Dovetail `dotnet test`.
- `dotnet run` of SdkSample prints the greeting through `IOptions<GreetingsOptions>`.
- With `DovetailConfigurationSchemaEndpoint=true`, `curl /.well-known/dovetail/configuration-schema` on SdkSample.Web returns the merged schema, including the Serilog section.
- A host without Optionary: the endpoint returns 404, and the build has no `appsettings.schema.json` and no warnings.

### Phase 4: delete the moved code (Dovetail PR)

1. **Delete:**
    - `src/Joinery.Dovetail.Analyzers/Support/Configuration/` and `ConfigurationGenerator`;
    - `src/Joinery.Dovetail.Build.Tasks/`, with its slnx entries;
    - the three `Joinery.Dovetail.ConfigurationSchema*.targets`;
    - `Directory.Packages.MSBuildTasks.props` and its renovate rule, if nothing else uses them;
    - the four moved test projects;
    - `ConfigurationOptionsTests`, `ConfigurationSchemaTests`, `KeyNamingTests` and `ConfigurationKeyStyleSources` with their snapshots, and the `KeyNaming.cs` link in the Analyzers.Tests csproj and its `.supports` projects;
    - the generic `Sdk.Tests` schema classes that moved to `Optionary.Build.Tests`.
2. **AnalyzerReleases.** Move DT0007–DT0016 and DT0018 to "Removed Rules".
3. **Docs.** `concepts/managed-configuration.md` becomes a short "Configuration with Optionary" page: add the package, `[Optionary]`, what Dovetail adds (the joint, SDK host detection, the endpoint and Serilog), and the rename table for upgraders. Each `plans/configuration-json-schema*.md` gets a header note pointing here.
4. **Release.** Dovetail's next minor (0.6.0) carries breaking notes listing D6/D7/D15 and the rename table.

**Verification**

- Build and full test.
- `rg -n 'DovetailConfiguration(Attribute|Schema)|Dovetail\.ConfigurationSchema' src test sample` finds nothing outside release notes and docs.

### Phase 5: stabilise (Optionary, then Dovetail)

Release Optionary `0.1.0` stable before Dovetail's next stable release, and bump Dovetail's pin. Any Dovetail nuspec or SDK reference to a prerelease would warn (NU5104).

## 7. History (D19: snapshot only)

- No git history is carried over. The import commit's message records:
    - the source: `RocketSurgeonsGuild/Dovetail@<import SHA>` (after Phase 0);
    - the path map:

        ```
        src/Joinery.Dovetail.Analyzers/Support/Configuration/**         -> src/Optionary.Analyzers/**
        src/Joinery.Dovetail.Build.Tasks/**                             -> src/Optionary.Build.Tasks/**
        src/Joinery.Dovetail/build/Joinery.Dovetail.ConfigurationSchema*.targets -> src/Optionary/build/Optionary.Schema*.targets
        src/Joinery.Dovetail/{DovetailConfigurationOptionsExtensions,Attributes/DovetailConfigurationAttribute,Attributes/ConfigurationKeyStyle,Infrastructure/DovetailOptionsFactory}.cs -> src/Optionary/...
        src/Joinery.Dovetail.Configuration.Yaml/**                      -> src/Optionary.Configuration.Yaml/** (removed again, 4.6)
        test/Joinery.Dovetail.{Build.Tasks,Configuration.Aot,Configuration.Parity,Configuration.Runtime}.Tests/** -> test/Optionary.{Build.Tasks,Aot,Parity,Runtime}.Tests/**
        ```

- The history before the import stays readable in Dovetail. Before #242 the paths were `src/Dovetail.*` and `src/Dovetail/*`, and the feature arrived in Dovetail#215 (2026-10-26). The README's "Origins" section links the permalink tree at the import SHA, plus PRs RocketSurgeonsGuild/Dovetail#215 and #242.

## 8. Test migration

### Moving to Optionary

| Optionary project                                                                   | From                                 | Content                                                                                                                                                                                 | Changes                                                                                                                                                                                                                                                                                                                                                     |
| ----------------------------------------------------------------------------------- | ------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `test/Optionary.Analyzers.Tests` (+ `.supports` roslyn5.0/5.3/5.6, now in the slnx) | `Joinery.Dovetail.Analyzers.Tests`   | `ConfigurationOptionsTests` (17 tests, 22 snapshots), `ConfigurationSchemaTests` (27 tests, 16 snapshots), `KeyNamingTests` (5), `ConfigurationKeyStyleSources`                         | Re-baseline for renames, the prefix and OPT IDs (schema snapshots change only by name). Options snapshots now hold `Optionary.Options.g.cs` only. `GeneratorTest` base, `ModuleInitializer` and `AddConfigurationOptionsReferences` move. Target framework is plain `net10.0`; Dovetail's uses `net10.0-browser` for its wasm joints.                       |
| `test/Optionary.Build.Tasks.Tests`                                                  | `Joinery.Dovetail.Build.Tasks.Tests` | 73 tests, 9 snapshots (without the Serilog tests), plus the hand-written-fragment test from Phase 0                                                                                     | Regenerate the 9 `generator-output/*.txt` copies from Optionary's analyzer snapshots.                                                                                                                                                                                                                                                                       |
| `test/Optionary.Aot.Tests`                                                          | `…Configuration.Aot.Tests`           | `GeneratedShapeRegistrationTests` and `GeneratedJointTests`, renamed `GeneratedRegistrationTests` (every test except `GeneratedConstructorParameterTests`)                              | `IsAotCompatible`, IL2026/IL3050/IL2091 as errors.                                                                                                                                                                                                                                                                                                          |
| `test/Optionary.Parity.Tests`                                                       | `…Configuration.Parity.Tests`        | `ParityTests` (6) and `KeyStyleTests` (9)                                                                                                                                               | `EnableConfigurationBindingGenerator`; net10.0 runs Binder 10, net11.0 runs Binder 11.                                                                                                                                                                                                                                                                      |
| `test/Optionary.Runtime.Tests`                                                      | `…Configuration.Runtime.Tests`       | Binding (6), Factory (8), MutableOptions (6), FileReload (1)                                                                                                                            | Drop the Json/Toml/Yaml/Dovetail references and use `Microsoft.Extensions.Configuration.Json`.                                                                                                                                                                                                                                                              |
| `test/Optionary.Build.Tests` (new, D20)                                             | `Joinery.Dovetail.Sdk.Tests`         | `ConfigurationSchemaTests` without `HostingOnlyHost…`, `…VisualStudioTests` (10), `…EditorSettingsTests` (7), `…IncrementalTests` (2), `…MultiTargetingTests` (2), `…FragmentTests` (1) | MSBuild.ProjectCreation, packing Optionary in-repo first. Port the SchemaStore scrubber (`SdkTestProject.cs:228-241`). New cases: OPT1001 when a target removes the generator from `@(Analyzer)`, and no OPT1001 with `ExcludeAssets="analyzers"` (the SDK still loads the generator); `OptionarySchemaCopyToOutput=false`; `OptionarySchemaHost` override. |

New contract tests in `Optionary.Analyzers.Tests` cover section 4.2:

- the method exists with zero declarations, with all declarations rejected, with every declaration `RegisterOptions=false`, and below C# 12;
- the namespace is `RootNamespace` verbatim, or the global namespace when it is empty;
- nothing is emitted when the attribute type comes from an assembly not named `Optionary`.

### Staying in or added to Dovetail

- **`Analyzers.Tests/OptionsJointTests`** (section 4.3):
    - the joint is first and calls the exact method;
    - no joint without declarations, below C# 12, or for a non-`Optionary` assembly;
    - the namespace comes from `RootNamespace`, not `DovetailExportNamespace`.

    It compiles a stub reference assembly named `Optionary`, so it doesn't depend on Optionary's generator.

- **`Analyzers.Tests`.** `MultiFormatConfigurationTests` and `EndToEndConfigurationScenarioTests` keep using Dovetail's own format packages. The latter is effectively dead: delete it or make it real.
- **`Sdk.Tests` (integration, against the pinned prerelease):**
    - `HostingOnlyHost_MergesTheirSections…`;
    - `ConfigurationSchemaSerilogTests` (4), `ConfigurationSchemaEndpointTests` (5) and `HostingHelperTests.AspNetCore_AlsoEmitsTheSchemaEndpointExtensions`;
    - `ZeroAllocJointTests.ValidatedConfiguration_IsValidatedWithZeroAllocValidationOptions`;
    - new: an SDK-detected host gets `OptionarySchemaHost`; a MAUI/Uno embedded-config host doesn't copy the schema; a library referencing `Joinery.Dovetail.Configuration.Yaml` exports no YAML joint while its app exports one; an app with an `[Optionary]` type resolves `IOptions<T>` through the Dovetail host, the end-to-end proof of section 4.
- **`test/Joinery.Dovetail.Configuration.Tests`** (from Phase 0): `EmbeddedConfigurationTests`.
- **Hosting tests** that reference YAML joints or providers, updated in Phase 0.

### How Dovetail tests against a prerelease

- **Versions.** `Directory.Packages.props` pins `Optionary`. `Sdk.Tests` scaffolds read the same version (passed through a generated constant or MSBuild property), so the real-build tests and the unit tests use one version.
- **Bumps.** Renovate picks up new prereleases. Each bump runs the full Dovetail suite, which is the drift alarm for section 4.
- **Local co-development.** `dotnet pack -o ~/.nuget/local-optionary` in Optionary, then an uncommitted `NuGet.config` override in Dovetail adding that folder with a `packageSourceMapping` entry for `Optionary*`. CI never sees it.

## 9. Risks and mitigations

| Risk                                                                                                            | Mitigation and check                                                                                                                                                                                                    |
| --------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Contract drift: strings in two repos                                                                            | The section 4 tests on both sides. Dovetail's `Sdk.Tests` end-to-end case builds against the real package on every bump. Contract changes require an Optionary major and a Dovetail release.                            |
| Double generation or double schema targets during the switch (CS0101, two writers of `appsettings.schema.json`) | Phase 3 is one PR that unregisters `ConfigurationGenerator` and removes the targets import in the same change as adding the package. Verified by the Phase 3 build and `Sdk.Tests`.                                     |
| Silent schema loss for upgraders (opt-in, D15)                                                                  | `[DovetailConfiguration]` no longer compiles (clean break), so options binding breaks loudly. Only the editor schema disappears quietly; the release notes and the Dovetail docs page cover it.                         |
| Libraries packed with the old prefix no longer merge (D7)                                                       | Accepted. Rebuilding with Optionary restores them. Release notes.                                                                                                                                                       |
| `RootNamespace` mismatch, or an invalid namespace                                                               | Both sides read `build_property.RootNamespace` verbatim, the same value Dovetail already uses for its exports. Tested on both sides, including an empty value.                                                          |
| Generator not loaded                                                                                            | OPT1001, plus a documented CS0234 fallback for excluded build assets.                                                                                                                                                   |
| New PublicAPI tracking                                                                                          | Generate the initial files with the code fix and review them in the rename commit. The generated method is internal, so it never shows up there.                                                                        |
| Binder drift across Binder 10 and 11                                                                            | The parity suite moves intact and runs on both target frameworks.                                                                                                                                                       |
| Infrastructure carried from a trimmed Dovetail                                                                  | Fresh slnx, no stale entries. CI green on the scaffold commit before the import.                                                                                                                                        |
| Third-party notices                                                                                             | The SchemaStore copy and the dotnet/runtime binder port (MIT, `BindingModel.cs` header) keep their notices in `Optionary.Build.Tasks/THIRD-PARTY-NOTICES.txt` and the package. Checked in the Phase 1 nupkg inspection. |
| Trusted publishing for a new repo                                                                               | A Phase 2 prerequisite, checked by restoring the published `beta.1`.                                                                                                                                                    |
| Unverified Visual Studio behaviour (P0 S1) is inherited                                                         | Unchanged by the move. It stays an open item in Optionary's docs.                                                                                                                                                       |

## 10. Out of scope (follow-ups)

- PublicAPI tracking in Dovetail and Specular (RS0017 does nothing there today).
- A Dovetail SDK switch that adds Optionary implicitly (declined for now).
- Binder 8/9 and `net8.0`/`net9.0` support.
- Visual Studio checks for segments (P0 S1).

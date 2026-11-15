# Rocket Surgeons Guild — Optionary Constitution

## Core Principles

### I. Reflection-Free Binding, Generated at Build Time

Options types marked `[Optionary]` (or registered with `[assembly: Optionary<T>("Section")]`) are bound by code that the Roslyn incremental source generator (`src/Optionary.Analyzers/`) writes at build time. Nothing scans assemblies or reflects over members at run time, so every binder is Native AOT and trimming safe. A shape the generator can't bind is a diagnostic (`OPT0001`–`OPT0012`), never a silent runtime fallback.

### II. One Schema per Application

Each library contributes JSON Schema fragments (assembly metadata prefixed `Optionary.Schema.v1:`); the MSBuild tasks (`src/Optionary.Build.Tasks/`) merge every fragment an application references into one `appsettings.schema.json`. Fragments flow transitively through the `buildTransitive/` assets, so a host gets completion for its libraries' options without extra wiring.

### III. Independent of Dovetail

Optionary knows nothing about Dovetail: no reference, no detection. The cross-repo contract is the attribute names and the generated `OptionaryConfigurationOptions.AddOptionaryConfigurationOptions(IServiceCollection)` method (plan section 4); changing either is a breaking change for Dovetail and is treated as public API. The only exception is `Optionary.Configuration.Yaml`, which declares Dovetail joint templates that do nothing without Dovetail.

### IV. Public API Surface is a Contract (RS0016, RS0017)

`Microsoft.CodeAnalysis.PublicApiAnalyzers` tracks every packable project. RS0016 (a public symbol missing from `PublicAPI.Unshipped.txt`) and RS0017 (a listed symbol that's gone) are **build errors**. New API goes into `PublicAPI.Unshipped.txt`; a release moves it to `PublicAPI.Shipped.txt`. Diagnostics follow the same rule through `AnalyzerReleases.Unshipped.md` / `AnalyzerReleases.Shipped.md`.

### V. Central Package Management, No Version Drift

All NuGet package versions live in `Directory.Packages.props`. `.csproj` files never specify versions (the per-Roslyn analyzer slices use `VersionOverride` on purpose). `GlobalPackageReference` entries apply analyzers and build tooling uniformly. The netstandard2.0 MSBuild task references stay pinned in `Directory.Packages.MSBuildTasks.props`. NuGet audit runs at `moderate` level on every build.

### VI. Snapshot Tests Guard the Generator and the Schema

Generated binders and schema fragments are verified against committed Verify snapshots. A failing snapshot means the generator output changed — review the diff, accept intentional changes with `dotnet verify accept`. Never accept snapshots blindly. Real-build tests (MSBuild.ProjectCreation) cover the merge targets end to end.

## Code Standards

- **Language**: C# with `LangVersion=preview`, `Nullable=enable`, `ImplicitUsings=enable`, `Features=strict`, `AnalysisMode=AllEnabledByDefault`
- **Targets**: runtime and YAML packages `net10.0;net11.0`; the generator `netstandard2.0`, built per Roslyn version (roslyn5.0/5.3/5.6 in `src/Optionary.Analyzers.supports/`, roslyn5.9 in `src/Optionary.Analyzers/`); the MSBuild tasks `netstandard2.0`
- **Packaging**: one `Optionary` package carries the runtime (`lib/`), the generator (`analyzers/dotnet/roslyn5.x/cs/`), the tasks (`build/tasks/netstandard2.0/`) and the MSBuild assets (`build/`, `buildTransitive/`, `buildMultiTargeting/`). `Optionary.Configuration.Yaml` doesn't depend on `Optionary`.
- **Global usings**: `JetBrains.Annotations`, `System.Diagnostics.CodeAnalysis`, `NotNullAttribute` alias applied to all projects
- **Warnings as errors**: `RS0016` and `RS0017` — do not silently suppress other warnings without justification
- **Formatting**: `hk fix` (dotnet format for C#, biome for JS/TS/JSON, oxfmt for YAML/Markdown/CSS, tombi for TOML); CI enforces it via `lint.yml`
- **No magic package versions in csproj** — always add to `Directory.Packages.props` first

## Development Workflow

**Toolchain**: `mise install` (the .NET 11 SDK, the .NET 10 runtime for the net10.0 tests, node, hk and the dotnet tools)

**Build**: `mise run build` (full CI pipeline via ModularPipelines in `build/Build.cs`), or `dotnet build Optionary.slnx`

**Test**:

- Framework: TUnit on Microsoft.Testing.Platform (not VSTest)
- Mocks: TUnit.Mocks · Assertions: TUnit Should syntax (`TUnit.Assertions.Should`) · Snapshots: Verify.TUnit
- Run all: `dotnet test --solution Optionary.slnx`
- Run single: `dotnet run --project test/<Project> -- --treenode-filter "*/..."`
- Accept snapshots: `dotnet verify accept`

**Docs**: `mise run docs` (Astro Starlight dev server in `docs/`); `mise run docs:build` builds the site

**Format**: `hk fix --all`

**Release**: GitVersion computes semver from branch/tag; GitReleaseManager drafts the release notes; a `v*` tag publishes to nuget.org through trusted publishing in `build.yml`.

**Dependency updates**: Renovate runs automatically; bot PRs are auto-merged for minor/patch after CI passes.

## Governance

This constitution supersedes all other practices in this repository. Amendments require updating this file and regenerating `AGENTS.md` / `CLAUDE.md` with `apm compile`. All PRs should verify compliance with the public API files, the analyzer release files, snapshot tests, and formatting checks before merge. Complexity must be justified — prefer extending existing code over adding new abstractions.

**Version**: 1.0.0 | **Ratified**: 2026-11-15 | **Last Amended**: 2026-11-15

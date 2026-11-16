# Optionary

Strongly-typed, AOT-safe configuration options for .NET: a reflection-free options binder and per-application JSON Schema for editor completion, generated from your options types.

It ships as one package, `Optionary`: the `[Optionary]` attribute, the source generator, the binder and the schema build.
Optionary reads whatever `IConfiguration` holds and adds no configuration providers of its own; use Microsoft's JSON
provider or any other `IConfigurationSource` for the files.

Documentation: <https://rocketsurgeonsguild.github.io/Optionary/>. Optionary is being extracted from Dovetail; see the
[migration plan](plans/migration-from-dovetail.md).

## Building

The toolchain comes from [mise](https://mise.jdx.dev): `mise install`, then `mise run build` (or `dotnet build Optionary.slnx`).
`mise run docs` serves the documentation site from `docs/`.

## Origins

Optionary started as the configuration schema and binding feature of
[Dovetail](https://github.com/RocketSurgeonsGuild/Dovetail), and was moved into its own library so it can be used without
Dovetail. The code was imported from Dovetail at commit
[`3f512a33b0`](https://github.com/RocketSurgeonsGuild/Dovetail/commit/3f512a33b0) as a snapshot, without its history, and
then renamed; Dovetail's history up to that commit is where to look for how it came to be.

# Optionary

Strongly-typed, AOT-safe configuration options for .NET: a reflection-free options binder and per-application JSON Schema for editor completion, generated from your options types.

| Package                        | What it is                                                                          |
| ------------------------------ | ----------------------------------------------------------------------------------- |
| `Optionary`                    | The `[Optionary]` attribute, the source generator, the binder and the schema build. |
| `Optionary.Configuration.Yaml` | A YAML configuration provider.                                                      |

> Optionary is being extracted from Dovetail; the code hasn't landed yet. See the
> [migration plan](plans/migration-from-dovetail.md).

## Building

The toolchain comes from [mise](https://mise.jdx.dev): `mise install`, then `mise run build` (or `dotnet build Optionary.slnx`).
`mise run docs` serves the documentation site from `docs/`.

## Origins

Optionary started as the configuration schema and binding feature of
[Dovetail](https://github.com/RocketSurgeonsGuild/Dovetail), and was moved into its own library so it can be used without
Dovetail.

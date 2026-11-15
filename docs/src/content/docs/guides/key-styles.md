---
title: Key styles
description: Spell the keys of an options type as declared, or in Pascal, camel, dash or underscore case.
tags: [configuration]
---

A declaration's **key style** picks how the keys of its options type, and of everything nested in it, are spelled. It is
set per declaration with `KeyStyle`, on `[Optionary]` and on `[assembly: Optionary<T>]`. There is no assembly-wide or
MSBuild default.

```csharp
[Optionary(KeyStyle = ConfigurationKeyStyle.Underscore)]
public class HttpClientOptions // section "http_client"
{
    public int MaxFileSizeMb { get; set; } // key "max_file_size_mb"
    public RetryPolicy Retry { get; set; } = new(); // "retry:max_attempts" through RetryPolicy.MaxAttempts
}
```

## The five styles

| `ConfigurationKeyStyle` | The key is                                           | `MaxFileSizeMb`    | `HTTPServer`  | `HttpClientOptions` section |
| ----------------------- | ---------------------------------------------------- | ------------------ | ------------- | --------------------------- |
| `Undefined` (default)   | The declared name exactly as written.                | `MaxFileSizeMb`    | `HTTPServer`  | `HttpClient`                |
| `Pascal`                | The declared name with its first letter upper-cased. | `MaxFileSizeMb`    | `HTTPServer`  | `HttpClient`                |
| `Camel`                 | Lower camel case (`JsonNamingPolicy.CamelCase`).     | `maxFileSizeMb`    | `httpServer`  | `httpClient`                |
| `Dash`                  | Lower-case words joined with `-` (`KebabCaseLower`). | `max-file-size-mb` | `http-server` | `http-client`               |
| `Underscore`            | Lower-case words joined with `_` (`SnakeCaseLower`). | `max_file_size_mb` | `http_server` | `http_client`               |

## What the style changes

- **`Undefined` respells nothing.** The binder, the schema's keys and the derived section path use the names as declared.
  Configuration keys ignore case, so `maxfilesizemb`, `MaxFileSizeMb` and `MAXFILESIZEMB` are the same key. Dashes and
  underscores are other characters, though: `max-file-size-mb` does not match `MaxFileSizeMb`.
- **`Pascal` is explicit.** C# property names already start in upper case, so for them `Undefined` and `Pascal` are the
  same. They differ for a member declared as `myKey`: it stays `myKey` under `Undefined` and becomes `MyKey` under `Pascal`.
- **What it covers.** The key of every bound property and constructor parameter of the options type and of every object
  type nested in it, in lists and dictionary values too. The generated binder reads those keys, and the schema uses them
  for its properties, its case-insensitive twins and the strict key check.
- **What it leaves alone.** Dictionary keys, enum values and other data are never restyled, and neither is an explicit
  section path: `"Http:Retry"` is used as written.
- **Word splitting** is `System.Text.Json`'s. `HTTPServer` becomes `httpServer`, `http-server` and `http_server`;
  `Http2Enabled` becomes `http2Enabled`, `http2-enabled` and `http2_enabled`.
- **An explicit key name always wins.** `[ConfigurationKeyName]` is used verbatim, in every style.
- **Derived section paths follow the style**, after the suffix is removed: `HttpClientOptions` is `HttpClient`,
  `httpClient`, `http-client` and `http_client`.

### Case versus letters

Because keys ignore case, `Camel` against `Undefined` or `Pascal` only changes the spelling the schema suggests:
`maxFileSizeMb` and `MaxFileSizeMb` bind the same value. `Dash` and `Underscore` change the characters the binder reads: a
value written as `MaxFileSizeMb` no longer binds (`max-file-size-mb` or `max_file_size_mb` does), and the schema only
accepts those keys, in any case.

## One spelling per type

The binder is generated once for each type. A type reached from sections that spell its keys differently, such as a
`RetryOptions` used by a camel section and a dash section, can't be bound correctly for both. The later declaration is
reported as [`OPT0011`](../../reference/diagnostics/#opt0011) and contributes nothing.

- The comparison is of the effective keys, not the style names. `Undefined` and `Pascal` don't conflict on ordinary names,
  which spell the same keys, but do on a member declared in lower case.
- Fix it by using the same `KeyStyle` for every section that reaches the type, or by giving the shared type
  `[ConfigurationKeyName]` attributes. A type whose keys don't depend on the style isn't a conflict.
- Declarations with `RegisterOptions = false` bind nothing, so they can't conflict.

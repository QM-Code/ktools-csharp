# API Guide

This page summarizes the public C# API in the `src/Kcli/` implementation,
primarily:

- [`src/Kcli/Parser.cs`](../src/Kcli/Parser.cs)
- [`src/Kcli/InlineParser.cs`](../src/Kcli/InlineParser.cs)
- [`src/Kcli/ParseEngine.cs`](../src/Kcli/ParseEngine.cs)

## Core Types

| Type | Purpose |
| --- | --- |
| `Parser` | Owns aliases, top-level handlers, positional handling, and inline parser registration. |
| `InlineParser` | Defines one inline root namespace such as `--build` plus its `--build-*` handlers. |
| `HandlerContext` | Metadata delivered to flag, value, and positional handlers. |
| `CliError` | Exception used by `ParseOrThrow()` for invalid CLI input and handler failures. |

## HandlerContext

`HandlerContext` is passed to every handler.

| Property | Meaning |
| --- | --- |
| `Root` | Inline root name without leading dashes, such as `build`. Empty for top-level handlers and positional dispatch. |
| `Option` | Effective option token after alias expansion, such as `--verbose` or `--build-profile`. Empty for positional dispatch. |
| `Command` | Normalized command name without leading dashes. Empty for positional dispatch and inline root value handlers. |
| `ValueTokens` | Effective value tokens after alias expansion. Shell tokens are preserved verbatim; alias preset tokens are prepended. |

## CliError

`ParseOrThrow()` throws `CliError` when:

- the command line is invalid
- a registered option handler throws
- the positional handler throws

`CliError.Option` returns the option token associated with the failure when one
exists. For positional-handler failures and parser-global errors, it may be
empty.

## InlineParser

### Construction

```csharp
InlineParser parser = new InlineParser("--build");
```

The root may be provided as either:

- `"build"`
- `"--build"`

### Root Value Handler

```csharp
parser.SetRootValueHandler(handler);
parser.SetRootValueHandler(handler, "<selector>", "Select build targets.");
```

The root value handler processes the bare root form, for example:

- `--build release`
- `--config user.json`

If the bare root is used without a value, `kcli` prints inline help for that
root instead.

### Inline Handlers

```csharp
parser.SetHandler("-flag", flagHandler, "Enable build flag.");
parser.SetHandler("-profile", valueHandler, "Set build profile.");
parser.SetOptionalValueHandler("-enable", optionalHandler, "Enable build mode.");
```

Inline handler options may be written in either form:

- short inline form: `-profile`
- fully-qualified form: `--build-profile`

### Root Changes

```csharp
parser.SetRoot("--newbuild");
```

`SetRoot()` replaces the inline root after construction. Existing command
handlers stay attached to the parser and will use the new root name.

## Parser

### Top-Level Handlers

```csharp
parser.SetHandler("--verbose", handleVerbose, "Enable verbose logging.");
parser.SetHandler("--output", handleOutput, "Set output target.");
parser.SetOptionalValueHandler("--color", handleColor, "Set or auto-detect color output.");
```

Top-level handler options may be written as either:

- `"verbose"`
- `"--verbose"`

### Aliases

```csharp
parser.AddAlias("-v", "--verbose");
parser.AddAlias("-c", "--config", "user-file");
```

Rules:

- aliases use single-dash form such as `-v`
- alias targets use double-dash form such as `--verbose`
- preset tokens are prepended to the handler's effective `ValueTokens`

### Positional Handler

```csharp
parser.SetPositionalHandler(handlePositionals);
```

The positional handler receives remaining non-option tokens in
`HandlerContext.ValueTokens`.

### Inline Parser Registration

```csharp
parser.AddInlineParser(buildParser);
```

Duplicate inline roots are rejected.

### Parse Entry Points

```csharp
parser.ParseOrExit(args);
parser.ParseOrThrow(args);
parser.ParseOrExit(argc, argv);
parser.ParseOrThrow(argc, argv);
```

`ParseOrExit()`

- preserves the caller's argument vector
- reports invalid CLI input to `stderr` as `[error] [cli] ...`
- exits with code `2`

`ParseOrThrow()`

- preserves the caller's argument vector
- throws `CliError`
- does not run handlers until the full command line validates

## Value Handler Registration

Use the registration form that matches the CLI contract you want:

- `SetHandler(option, FlagHandler, description)` for flag-style options
- `SetHandler(option, ValueHandler, description)` for required values
- `SetOptionalValueHandler(option, ValueHandler, description)` for optional values
- `SetRootValueHandler(...)` for bare inline roots such as `--build release`

## API Notes

- `Parser` and `InlineParser` are mutable registration objects.
- Optional-value handlers receive an empty string when no value is provided.
- Literal `--` is not a terminator; it is treated as an unknown option.

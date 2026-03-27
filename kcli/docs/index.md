# Kcli C# Documentation

`kcli` is the C# implementation of the ktools command-line parsing layer.

It keeps the same core model used across the ecosystem:

- one `Parser` for top-level options and aliases
- one `InlineParser` per inline root such as `--build` or `--trace`
- full command-line validation before any handler runs
- explicit failure through `CliError` or exit-oriented parsing

## Start Here

- [API guide](api.md)

## Typical Flow

```csharp
using Kcli;

Parser parser = new Parser();
InlineParser build = new InlineParser("--build");

build.SetHandler("-profile", (context, value) => {
}, "Set build profile.");

parser.AddInlineParser(build);
parser.AddAlias("-v", "--verbose");
parser.SetHandler("--verbose", context => {
}, "Enable verbose logging.");

parser.ParseOrExit(args);
```

## Core Concepts

`Parser`

- Owns top-level handlers, aliases, positional handling, and inline parser registration.

`InlineParser`

- Defines one inline root namespace such as `--alpha`, `--build`, or `--trace`.

`HandlerContext`

- Exposes the effective root, option, command, and value tokens seen by the handler after alias expansion.

`CliError`

- Used by `ParseOrThrow()` to surface invalid CLI input and handler failures.

## Which Entry Point Should I Use?

Use `ParseOrExit()` when:

- you are in a normal executable entrypoint
- invalid CLI input should print a standardized error and exit with code `2`
- you do not need custom error handling

Use `ParseOrThrow()` when:

- you want custom formatting or exit behavior
- you want to test parse failures directly
- you want to intercept handler failures as exceptions

## Working References

- [`src/Kcli/Kcli.cs`](../src/Kcli/Kcli.cs)
- [`tests/src/Kcli.Tests/Program.cs`](../tests/src/Kcli.Tests/Program.cs)
- [`demo/bootstrap/src/Kcli/Demo/Bootstrap/Program.cs`](../demo/bootstrap/src/Kcli/Demo/Bootstrap/Program.cs)
- [`demo/exe/core/src/Kcli/Demo/Core/Program.cs`](../demo/exe/core/src/Kcli/Demo/Core/Program.cs)
- [`demo/exe/omega/src/Kcli/Demo/Omega/Program.cs`](../demo/exe/omega/src/Kcli/Demo/Omega/Program.cs)

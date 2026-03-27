# Karma CLI Parsing SDK For C#

`kcli` is a small C# SDK for building structured command-line interfaces.
It supports the same two CLI shapes used elsewhere in the ktools stack:

- top-level options such as `--verbose`
- inline roots such as `--trace-*`, `--config-*`, and `--build-*`

The C# API keeps the same conceptual model as the existing C++ and Java
implementations while using .NET-style naming.

## Quick Start

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

## Documentation

- [Overview](docs/index.md)
- [API guide](docs/api.md)

## Behavior Highlights

- The full command line is validated before any registered handler runs.
- `ParseOrExit()` prints `[error] [cli] ...` to `stderr` and exits with code `2`.
- `ParseOrThrow()` raises `CliError`.
- Bare inline roots print inline help when no root value is provided.
- Required values may consume an option-like first token.
- Literal `--` remains an unknown option; it is not treated as a separator.

## Build

```bash
python3 ../kbuild/kbuild.py --build-latest
```

## Layout

- Public API and implementation: `src/`
- API tests: `tests/src/`
- Demo builds: `demo/`

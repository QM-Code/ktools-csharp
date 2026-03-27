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
- [Parsing behavior](docs/behavior.md)
- [Examples](docs/examples.md)

## Behavior Highlights

- The full command line is validated before any registered handler runs.
- `ParseOrExit()` prints `[error] [cli] ...` to `stderr` and exits with code `2`.
- `ParseOrThrow()` raises `CliError`.
- Bare inline roots print inline help when no root value is provided.
- Required values may consume an option-like first token.
- Literal `--` remains an unknown option; it is not treated as a separator.

## Build

```bash
kbuild --build-latest
```

## Build And Run Demos

```bash
# Builds the SDK plus demos listed in .kbuild.json build.defaults.demos.
kbuild --build-latest

# Explicit demo-only run.
kbuild --build-demos
```

Demo directories:

- Bootstrap compile/link check: `demo/bootstrap/`
- SDK demos: `demo/sdk/{alpha,beta,gamma}`
- Executable demos: `demo/exe/{core,omega}`

Useful demo commands:

```bash
./demo/exe/core/build/latest/test
./demo/exe/core/build/latest/test --alpha
./demo/exe/core/build/latest/test --alpha-message "hello"
./demo/exe/core/build/latest/test --output stdout
./demo/exe/omega/build/latest/test --beta-workers 8
./demo/exe/omega/build/latest/test --newgamma-tag "prod"
./demo/exe/omega/build/latest/test --build
```

## Layout

- Public API and implementation: `src/`
- API tests: `tests/src/`
- Demo builds: `demo/`

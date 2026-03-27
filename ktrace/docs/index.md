# Ktrace C# Documentation

`ktrace` is the C# implementation of the ktools tracing and logging layer.

It is built around two cooperating types:

- `TraceLogger` for namespace-scoped trace sources
- `Logger` for executable-facing channel registration, filtering, formatting, and output

## Start Here

- [API guide](api.md)
- [Behavior guide](behavior.md)
- [Examples](examples.md)

## Typical Flow

```csharp
using Kcli;
using Ktrace;

Logger logger = new Logger();
TraceLogger appTrace = new TraceLogger("core");
appTrace.AddChannel("app", "BrightCyan");

logger.AddTraceLogger(appTrace);

Parser parser = new Parser();
parser.AddInlineParser(logger.MakeInlineParser(appTrace));
parser.ParseOrExit(args);
```

## Core Concepts

`TraceLogger`

- Declares one trace namespace and its registered channels.
- Emits channel-based trace output through `Trace()` and `TraceChanged()`.
- Emits always-visible operational logging through `Info()`, `Warn()`, and `Error()`.
- Applies explicit channel colors, with nested channels inheriting their nearest colored parent.

`Logger`

- Imports one or more `TraceLogger` instances.
- Maintains the runtime registry of namespaces and channels.
- Enables or disables channels by exact selector or selector list.
- Owns output formatting and the `--trace-*` CLI integration surface.

`OutputOptions`

- Controls optional filename, line, function, and timestamp output.

`TraceFormatter`

- Provides public brace-based message formatting and exposes the named color catalog used by `ktrace`.

## Selector Model

Single-channel APIs accept:

- `.channel[.sub[.sub]]` for the local namespace supplied by the caller
- `namespace.channel[.sub[.sub]]` for an explicit namespace

Selector-list APIs accept:

- `*`
- brace groups such as `{alpha,beta}`
- comma-separated selector lists

Examples:

- `.app`
- `alpha.net`
- `alpha.*`
- `*.*.*`
- `*.{net,io}`
- `{alpha,beta}.scheduler.tick`

## Color Model

- `TraceLogger.AddChannel(name, colorName)` accepts `Default` plus the xterm-style named colors shared with the C++ implementation.
- Child channels inherit the nearest registered parent color unless they declare their own color.
- `--trace-colors` prints the full available catalog at runtime.

## Working References

- [`src/Ktrace/Ktrace.cs`](../src/Ktrace/Ktrace.cs)
- [`tests/src/Ktrace.Tests/Program.cs`](../tests/src/Ktrace.Tests/Program.cs)
- [`demo/exe/core/src/Ktrace/Demo/Core/Program.cs`](../demo/exe/core/src/Ktrace/Demo/Core/Program.cs)
- [`demo/exe/omega/src/Ktrace/Demo/Omega/Program.cs`](../demo/exe/omega/src/Ktrace/Demo/Omega/Program.cs)

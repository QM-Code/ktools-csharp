# Karma Trace Logging SDK For C#

`ktrace` is the C# tracing and logging layer in the ktools ecosystem.

It provides:

- channel-based trace output
- always-visible operational logging through `Info`, `Warn`, and `Error`
- selector-driven runtime enablement
- `kcli` integration through `Logger.MakeInlineParser(...)`

## Quick Start

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

## Documentation

- [Overview](docs/index.md)
- [API guide](docs/api.md)
- [Behavior guide](docs/behavior.md)
- [Examples](docs/examples.md)

## Behavior Highlights

- Trace channels are registered explicitly on `TraceLogger`.
- Runtime enablement happens through exact selectors or selector lists.
- Unregistered channels never emit, even if a selector pattern would otherwise match.
- Child channels inherit the nearest registered parent color unless they override it explicitly.
- Output options apply to both trace output and operational logging.
- Named colors include `Default` plus the C++-style xterm 256-color catalog.

## Build

```bash
kbuild --build-latest
```

From the workspace root, prefer:

```bash
cd ..
kbuild --batch --build-latest
```

`ktrace/` depends on the sibling `kcli/` SDK. Component-local `ktrace`
builds expect `../kcli/build/<slot>/sdk/lib/Kcli.dll` to already exist.

SDK output:

- `build/latest/sdk/lib/Ktrace.dll`
- `build/latest/tests/bin/Ktrace.Tests.dll`

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

Trace CLI examples:

```bash
./demo/exe/core/build/latest/test --trace
./demo/exe/core/build/latest/test --trace '.*'
./demo/exe/omega/build/latest/test --trace '*.*'
./demo/exe/omega/build/latest/test --trace '*.*.*.*'
./demo/exe/omega/build/latest/test --trace '*.{net,io}'
./demo/exe/omega/build/latest/test --trace-namespaces
./demo/exe/omega/build/latest/test --trace-channels
./demo/exe/omega/build/latest/test --trace-colors
```

## Run Tests

```bash
dotnet build/latest/tests/bin/Ktrace.Tests.dll
```

## Layout

- Public API and implementation: `src/Ktrace/`
- Trace CLI integration split across `Logger.cs`, `Logger.TraceCli.cs`, and `TraceCliRenderer.cs`
- API tests: `tests/src/Ktrace.Tests/`
- Demo builds: `demo/`

Working references:

- `src/Ktrace/TraceLogger.cs`
- `src/Ktrace/Logger.cs`
- `src/Ktrace/Logger.TraceCli.cs`
- `src/Ktrace/TraceSelector.cs`
- `src/Ktrace/TraceFormatter.cs`
- `src/Ktrace/TraceCliRenderer.cs`
- `tests/src/Ktrace.Tests/KtraceTests.cs`
- `tests/src/Ktrace.Tests/ChannelTests.cs`
- `tests/src/Ktrace.Tests/CliTests.cs`
- `tests/src/Ktrace.Tests/BootstrapDemoTests.cs`
- `tests/src/Ktrace.Tests/CoreDemoTests.cs`
- `tests/src/Ktrace.Tests/OmegaDemoTests.cs`
- `demo/sdk/alpha/src/Ktrace/Demo/Alpha/AlphaTrace.cs`
- `demo/exe/core/src/Ktrace/Demo/Core/Program.cs`
- `demo/exe/omega/src/Ktrace/Demo/Omega/Program.cs`

## API Model

`TraceLogger` is the namespace-bearing source object. Construct it with an
explicit namespace and declare channels on it:

```csharp
TraceLogger trace = new TraceLogger("alpha");
trace.AddChannel("net", "DeepSkyBlue1");
trace.AddChannel("cache", "Gold3");
```

SDKs should usually expose a shared handle from `GetTraceLogger()`:

```csharp
private static readonly TraceLogger Trace = CreateTraceLogger();

public static TraceLogger GetTraceLogger()
{
    return Trace;
}
```

`Logger` is the executable-facing runtime. It imports one or more
`TraceLogger`s, maintains the central channel registry, and owns filtering,
formatting, and final output:

```csharp
Logger logger = new Logger();

TraceLogger appTrace = new TraceLogger("core");
appTrace.AddChannel("app", "BrightCyan");
appTrace.AddChannel("startup", "BrightYellow");

logger.AddTraceLogger(appTrace);
logger.AddTraceLogger(AlphaTrace.GetTraceLogger());
```

## Logging APIs

Channel-based trace output:

```csharp
trace.Trace("channel", "message {}", value);
trace.TraceChanged("channel", key, "message {}", value);
```

Always-visible operational logging:

```csharp
trace.Info("message");
trace.Warn("configuration '{}' was not found", path);
trace.Error("fatal startup failure");
```

Operational logging is independent of channel enablement. It is still
namespaced and uses the same formatting options as trace output.

## CLI Integration

The inline parser is logger-bound rather than global. Pass the executable's
local `TraceLogger` so leading-dot selectors resolve against the right
namespace:

```csharp
Logger logger = new Logger();
TraceLogger appTrace = new TraceLogger("core");
appTrace.AddChannel("app", "BrightCyan");

logger.AddTraceLogger(appTrace);

Parser parser = new Parser();
parser.AddInlineParser(logger.MakeInlineParser(appTrace));
parser.ParseOrExit(args);
```

## Channel Expression Forms

Single-selector APIs on `Logger`:

- `.channel[.sub[.sub]]` for a local channel in the provided local namespace
- `namespace.channel[.sub[.sub]]` for an explicit namespace

List APIs on `Logger`:

- `EnableChannels(...)`
- `DisableChannels(...)`
- list APIs accept selector patterns such as `*`, `{}`, and CSV
- list APIs resolve selectors against the channels currently registered at call time
- leading-dot selectors in list APIs resolve against the provided local namespace
- empty or whitespace selector lists are rejected
- unregistered channels remain disabled and do not emit, even if a selector pattern would otherwise match

Examples:

- `logger.EnableChannel(appTrace, ".app");`
- `logger.EnableChannel("alpha.net");`
- `logger.EnableChannels("alpha.*,{beta,gamma}.net.*");`
- `logger.EnableChannels(appTrace, ".net.*,otherapp.scheduler.tick");`

Formatting options:

- `--trace-files`
- `--trace-functions`
- `--trace-timestamps`

These affect both `Trace(...)` output and `Info()/Warn()/Error()` output.

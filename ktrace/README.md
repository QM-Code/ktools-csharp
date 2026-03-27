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

## Behavior Highlights

- Trace channels are registered explicitly on `TraceLogger`.
- Runtime enablement happens through exact selectors or selector lists.
- Unregistered channels never emit, even if a selector pattern would otherwise match.
- Output options apply to both trace output and operational logging.

## Build

```bash
python3 ../kbuild/kbuild.py --build-latest
```

## Layout

- Public API and implementation: `src/`
- API tests: `tests/src/`
- Demo builds: `demo/`

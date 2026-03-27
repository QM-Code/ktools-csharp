# API Guide

This page summarizes the public C# API in
[`src/Ktrace/Ktrace.cs`](../src/Ktrace/Ktrace.cs).

## Core Types

| Type | Purpose |
| --- | --- |
| `TraceLogger` | Namespace-scoped trace source that declares channels and emits trace/log output. |
| `Logger` | Runtime registry, channel filter, formatter, and sink for one or more `TraceLogger` instances. |
| `OutputOptions` | Output-format options shared by trace output and operational logging. |
| `TraceFormatter` | Public formatting helper for brace-based trace message formatting. |

## TraceLogger

### Construction

```csharp
TraceLogger trace = new TraceLogger("alpha");
```

The namespace must be a non-empty selector identifier.

### Channel Registration

```csharp
trace.AddChannel("net");
trace.AddChannel("cache", "Gold3");
trace.AddChannel("scheduler.tick");
```

Rules:

- channel depth is limited to three segments
- nested channels require their parent channel to be registered first
- color names are optional

### Trace APIs

```csharp
trace.Trace("net", "connected to {}", host);
trace.TraceChanged("scheduler.tick", key, "tick {}", count);
```

`TraceChanged()` suppresses duplicate emissions for the same call site and key.

### Operational Logging

```csharp
trace.Info("starting up");
trace.Warn("configuration '{}' was not found", path);
trace.Error("fatal startup failure");
```

Operational logging is independent of channel enablement.

### Query API

```csharp
bool enabled = trace.ShouldTraceChannel("net");
```

This returns `true` only when the trace logger is attached to a `Logger`, the
channel is registered, and the channel is currently enabled.

## Logger

### Trace Logger Registration

```csharp
logger.AddTraceLogger(appTrace);
logger.AddTraceLogger(alphaTrace);
```

Attaching the same `TraceLogger` to multiple `Logger` instances is rejected.

### Exact Channel Control

```csharp
logger.EnableChannel("alpha.net");
logger.EnableChannel(appTrace, ".app");
logger.DisableChannel("alpha.net");
```

Exact selectors that do not match a registered channel are ignored with a
warning log.

### Selector-List Control

```csharp
logger.EnableChannels("alpha.*,{beta,gamma}.net.*");
logger.EnableChannels(appTrace, ".net.*,otherapp.scheduler.tick");
logger.DisableChannels("*.*.*");
```

Rules:

- empty selector lists are rejected
- unmatched selectors are ignored with warning logs
- unregistered channels remain disabled, even if a selector pattern would otherwise match them

### Output Formatting

```csharp
logger.SetOutputOptions(new OutputOptions
{
    Filenames = true,
    LineNumbers = true,
    FunctionNames = true,
    Timestamps = true,
});

OutputOptions options = logger.GetOutputOptions();
```

`FunctionNames` and `LineNumbers` only take effect when `Filenames` is enabled.

### Registry Queries

```csharp
List<string> namespaces = logger.GetNamespaces();
List<string> channels = logger.GetChannels("alpha");
bool enabled = logger.ShouldTraceChannel("alpha.net");
```

`GetChannels()` validates the namespace argument and returns sorted channel
names for that namespace.

### CLI Integration

```csharp
Parser parser = new Parser();
parser.AddInlineParser(logger.MakeInlineParser(appTrace));
```

`MakeInlineParser()` registers the standard trace CLI surface:

- `--trace <channels>`
- `--trace-examples`
- `--trace-namespaces`
- `--trace-channels`
- `--trace-colors`
- `--trace-files`
- `--trace-functions`
- `--trace-timestamps`

The inline parser is logger-bound. Leading-dot selectors resolve against the
namespace of the supplied local `TraceLogger`.

## OutputOptions

| Field | Meaning |
| --- | --- |
| `Filenames` | Include a source label in output. |
| `LineNumbers` | Include source line numbers when `Filenames` is enabled. |
| `FunctionNames` | Include member names when `Filenames` is enabled. |
| `Timestamps` | Include compact UTC timestamps in output. |

## TraceFormatter

```csharp
string text = TraceFormatter.FormatMessage("value {} {}", 7, "done");
```

Formatting rules:

- sequential `{}` placeholders
- escaped braces `{{` and `}}`
- `bool` formats as `true` or `false`
- too few or too many arguments raise `ArgumentException`

## API Notes

- `TraceLogger` is the library-facing type; `Logger` is the executable-facing runtime.
- Channel and namespace validation is strict and intentionally aligned with the broader ktools model.
- The docs here describe the current C# surface; the authoritative implementation is still [`src/Ktrace/Ktrace.cs`](../src/Ktrace/Ktrace.cs).

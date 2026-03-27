# Bootstrap Demo

Exists for CI and as the smallest compile/link usage reference for KtraceSDK.

This demo shows the minimal executable-side setup:

- create a `Ktrace.Logger`
- create a local `Ktrace.TraceLogger("bootstrap")`
- add one or more channels
- `logger.AddTraceLogger(...)`
- enable local selectors through `logger.EnableChannel(traceLogger, ".channel")`
- emit with `traceLogger.Trace(...)`

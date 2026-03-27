# Core Demo

Basic local-plus-imported tracing showcase for KtraceSDK and the alpha demo SDK.

This demo shows:

- executable-local tracing defined with a local `Ktrace.TraceLogger`
- imported SDK tracing added via `AlphaTrace.GetTraceLogger()`
- logger-managed selector state and output formatting
- local CLI integration through `parser.AddInlineParser(logger.MakeInlineParser(localTraceLogger))`

# Examples

This page shows a few common `ktrace` patterns. For complete compiling examples,
also see:

- [`../demo/sdk/alpha/src/Ktrace/Demo/Alpha/AlphaTrace.cs`](../demo/sdk/alpha/src/Ktrace/Demo/Alpha/AlphaTrace.cs)
- [`../demo/sdk/beta/src/Ktrace/Demo/Beta/BetaTrace.cs`](../demo/sdk/beta/src/Ktrace/Demo/Beta/BetaTrace.cs)
- [`../demo/sdk/gamma/src/Ktrace/Demo/Gamma/GammaTrace.cs`](../demo/sdk/gamma/src/Ktrace/Demo/Gamma/GammaTrace.cs)
- [`../demo/exe/core/src/Ktrace/Demo/Core/Program.cs`](../demo/exe/core/src/Ktrace/Demo/Core/Program.cs)
- [`../demo/exe/omega/src/Ktrace/Demo/Omega/Program.cs`](../demo/exe/omega/src/Ktrace/Demo/Omega/Program.cs)

## Minimal Executable

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

## Imported SDK Pattern

```csharp
Logger logger = new Logger();
TraceLogger appTrace = new TraceLogger("omega");
appTrace.AddChannel("app", "BrightCyan");
appTrace.AddChannel("orchestrator", "BrightYellow");

logger.AddTraceLogger(appTrace);
logger.AddTraceLogger(AlphaTrace.GetTraceLogger());
logger.AddTraceLogger(BetaTrace.GetTraceLogger());
```

This keeps local executable tracing separate from imported SDK trace namespaces.

## Selector Examples

```csharp
logger.EnableChannel(appTrace, ".app");
logger.EnableChannel("alpha.net");
logger.EnableChannels("alpha.*,{beta,gamma}.metrics");
logger.DisableChannels(appTrace, ".orchestrator");
```

## Output Options

```csharp
logger.SetOutputOptions(new OutputOptions
{
    Filenames = true,
    LineNumbers = true,
    FunctionNames = true,
    Timestamps = true,
});
```

This affects both `Trace(...)` output and `Info()/Warn()/Error()` output.

## Color Inheritance

```csharp
TraceLogger trace = new TraceLogger("alpha");
trace.AddChannel("net", "DeepSkyBlue1");
trace.AddChannel("net.retry");
trace.AddChannel("net.retry.deep");
```

Here, `net.retry` and `net.retry.deep` inherit `DeepSkyBlue1` until a more
specific color is registered.

# Behavior Guide

This page describes the `ktrace` runtime behavior that matters in practice.

## Registration Model

`TraceLogger` declares namespaces and channels up front.

Rules:

- namespaces must be non-empty selector identifiers
- channel depth is limited to three segments
- nested channels require their parent channel to be registered first
- adding the same channel again is allowed when the effective color does not conflict
- conflicting explicit colors for the same namespace and channel are rejected

## Enablement Model

`Logger` controls runtime trace enablement.

Exact selector APIs:

- `EnableChannel(...)`
- `DisableChannel(...)`
- `ShouldTraceChannel(...)`

Selector-list APIs:

- `EnableChannels(...)`
- `DisableChannels(...)`

Important details:

- exact selectors that do not resolve to a registered channel are ignored with a warning log
- selector lists resolve against channels registered at the time of the call
- unmatched selectors in a list are ignored with warning logs
- unregistered channels remain disabled, even if a wildcard selector would otherwise match them

## Selector Forms

Exact selectors accept:

- `.channel[.sub[.sub]]` for the local namespace
- `namespace.channel[.sub[.sub]]` for an explicit namespace

Selector lists also accept:

- `*`
- brace groups such as `{alpha,beta}`
- comma-separated selector expressions

Examples:

- `.app`
- `alpha.net`
- `alpha.*`
- `*.*.*`
- `*.{net,io}`
- `{alpha,beta}.scheduler.tick`

## Logging Behavior

`TraceLogger.Trace(...)`

- emits only when the channel is registered, attached to a `Logger`, and enabled

`TraceLogger.TraceChanged(...)`

- suppresses repeated emissions when the same call site repeats the same key
- keeps its duplicate-suppression state per trace logger

`TraceLogger.Info()/Warn()/Error()`

- always emit when the trace logger is attached to a `Logger`
- do not depend on channel enablement

## Colors

Color registration rules:

- channel colors are optional
- `Default` means no explicit color
- child channels inherit the nearest registered parent color unless they declare their own color
- the named color surface matches the C++ implementation's xterm-style catalog

Runtime behavior:

- ANSI colors are disabled when stdout is redirected
- `--trace-colors` prints the full available color catalog

## Output Options

`Logger.SetOutputOptions(...)` controls formatting shared by trace output and operational logging.

Fields:

- `Filenames`
- `LineNumbers`
- `FunctionNames`
- `Timestamps`

Important details:

- `LineNumbers` and `FunctionNames` only take effect when `Filenames` is enabled
- `--trace-files`, `--trace-functions`, and `--trace-timestamps` toggle the same runtime output settings

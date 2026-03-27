# ktools-csharp

`ktools-csharp/` is the C# workspace for the broader ktools ecosystem.

It is the root entrypoint for the C# implementations of the ktools libraries.

## Current Contents

- `kcli/`
  C# implementation of the CLI parsing layer.
- `ktrace/`
  C# implementation of the tracing/logging layer built on top of `kcli`.

The shared build tool is `kbuild`, expected on `PATH`. If you need to modify
the shared build implementation itself, that repo lives at [`../kbuild`](../kbuild).

## Build Model

Use `kbuild` from `PATH` as the workspace entrypoint:

```bash
kbuild --batch --build-latest
kbuild --batch --clean-latest
```

Use an individual child repo when you only need one SDK:

```bash
cd kcli
kbuild --build-latest
```

The C# backend currently expects the `dotnet` CLI to be available for actual builds.

## Where To Go Next

Use the child repo docs for API and implementation details:

- [kcli](kcli)
- [ktrace](ktrace)

# ktools-csharp

`ktools-csharp/` is the C# workspace for the broader ktools ecosystem.

It is the root entrypoint for the C# implementations of the ktools libraries.

## Current Contents

- `kbuild/`
  Local C#-workspace copy of the shared build tool, extended modularly for C#.
- `kcli/`
  C# implementation of the CLI parsing layer.
- `ktrace/`
  C# implementation of the tracing/logging layer built on top of `kcli`.

## Build Model

Use the local `kbuild` copy as the workspace entrypoint:

```bash
python3 kbuild/kbuild.py --batch --build-latest
python3 kbuild/kbuild.py --batch --clean-latest
```

Use an individual child repo when you only need one SDK:

```bash
cd kcli
python3 ../kbuild/kbuild.py --build-latest
```

The C# backend currently expects the `dotnet` CLI to be available for actual builds.

## Where To Go Next

Use the child repo docs for API and implementation details:

- [kcli](kcli)
- [ktrace](ktrace)

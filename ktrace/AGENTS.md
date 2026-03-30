# ktrace-csharp

Assume these have already been read:

- `../../ktools/AGENTS.md`
- `../AGENTS.md`

`ktools-csharp/ktrace/` is the C# implementation of `ktrace`.

## What This Component Owns

This component owns the C# API and implementation details for `ktrace`, including:

- trace/logger API behavior
- selector parsing and channel enablement semantics
- `kcli` integration through `--trace-*`
- C# demos and tests
- workspace C# build config for `kbuild`

## Local Bootstrap

When familiarizing yourself with this component, read:

- [README.md](README.md)
- `src/*`
- `tests/*`
- `demo/*`

## Build And Test Expectations

- Use `kbuild` from the component root for normal builds.
- Preserve cross-language trace behavior where possible.
- Keep CLI-facing trace controls aligned with the overview repo and the existing C++ implementation.

Useful commands:

```bash
kbuild --build-latest
kbuild --build-demos
kbuild --clean-latest
```

When working inside `ktrace/`, remember that the generated C# build expects the
sibling `../kcli/build/<slot>/sdk/lib/Kcli.dll` to exist. Build `../kcli/`
first or use the workspace-root `kbuild --batch --build-latest` flow.
After a coherent batch of changes in `ktools-csharp/ktrace/`, return to the
`ktools-csharp/` workspace root and run `kbuild --git-sync "<message>"`.

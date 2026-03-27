# ktrace-csharp

Assume these have already been read:

- `../../ktools/AGENTS.md`
- `../../ktrace/AGENTS.md`
- `../AGENTS.md`

`ktools-csharp/ktrace/` is the C# implementation of `ktrace`.

## What This Repo Owns

This repo owns the C# API and implementation details for `ktrace`, including:

- trace/logger API behavior
- selector parsing and channel enablement semantics
- `kcli` integration through `--trace-*`
- C# demos and tests
- repo-local C# build config for `kbuild`

## Local Bootstrap

When familiarizing yourself with this repo, read:

- [README.md](README.md)
- `src/*`
- `tests/*`
- `demo/*`

## Build And Test Expectations

- Use the local C# workspace `kbuild` copy for normal builds.
- Preserve cross-language trace behavior where possible.
- Keep CLI-facing trace controls aligned with the overview repo and the existing C++ implementation.

Useful commands:

```bash
python3 ../kbuild/kbuild.py --build-latest
python3 ../kbuild/kbuild.py --build-demos
python3 ../kbuild/kbuild.py --clean-latest
```

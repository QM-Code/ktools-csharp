# kcli-csharp

Assume these have already been read:

- `../../ktools/AGENTS.md`
- `../../kcli/AGENTS.md`
- `../AGENTS.md`

`ktools-csharp/kcli/` is the C# implementation of `kcli`.

## What This Repo Owns

This repo owns the C# API and implementation details for `kcli`, including:

- public C# parser APIs
- top-level and inline parsing behavior
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
- Prefer API checks plus demo-driven coverage.
- Keep behavior aligned with the cross-language `kcli` contract unless there is a strong C# reason not to.

Useful commands:

```bash
python3 ../kbuild/kbuild.py --build-latest
python3 ../kbuild/kbuild.py --build-demos
python3 ../kbuild/kbuild.py --clean-latest
```

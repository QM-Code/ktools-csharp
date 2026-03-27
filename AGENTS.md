# ktools-csharp

Assume `../ktools/AGENTS.md` has already been read.

`ktools-csharp/` is the C# workspace for the ktools ecosystem.

## What This Level Owns

This workspace owns C#-specific concerns such as:

- solution/project layout
- C# build and test flow
- C#-specific API naming and integration patterns
- coordination across C# tool implementations when more than one repo is present

Cross-language conceptual definitions belong at the overview/spec level, not here.

## Current Scope

This workspace currently contains:

- the shared build repo is a sibling checkout at `../kbuild/`
- `kcli/`
- `ktrace/`

## Guidance For Agents

1. First determine whether the task belongs at the workspace root or inside a specific implementation repo.
2. Prefer making changes in the narrowest repo that actually owns the behavior.
3. Use `kbuild` from `PATH` for normal builds. Do not invoke `kbuild.py` directly from this workspace.
4. Use the root workspace only for C#-workspace-wide concerns such as batch orchestration, local build-tooling adaptations, or root documentation.
5. Read the relevant child repo `AGENTS.md` and `README.md` files before changing code in that repo.
6. If shared build tooling needs C#-specific adaptations, change the sibling `../kbuild/` repo and document the delta for later consolidation.

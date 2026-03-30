# ktools-csharp

Assume `../ktools/AGENTS.md` has already been read.

`ktools-csharp/` is the C# workspace for the ktools ecosystem.

## What This Level Owns

This workspace owns C#-specific concerns such as:

- solution/project layout
- C# build and test flow
- C#-specific API naming and integration patterns
- coordination across C# tool implementations when more than one component is present

Cross-language conceptual definitions belong at the overview/spec level, not here.

## Current Scope

This workspace currently contains:

- the shared build repo is a sibling checkout at `../kbuild/`
- `kcli/`
- `ktrace/`

## Guidance For Agents

1. First determine whether the task belongs at the workspace root or inside a specific implementation component.
2. Prefer making changes in the narrowest component that actually owns the behavior.
3. Use `kbuild` from `PATH` for normal builds. Do not invoke `kbuild.py` directly from this workspace.
4. Use the root workspace only for C#-workspace-wide concerns such as batch orchestration, local build-tooling adaptations, or root documentation.
5. Read the relevant child component `AGENTS.md` and `README.md` files before changing code in that component.
6. If shared build tooling needs C#-specific adaptations, change the sibling `../kbuild/` repo and document the delta for later consolidation.

## Git Sync

Use the shared `kbuild` workflow for commit/push sync from this workspace root:

```bash
kbuild --git-sync "<message>"
```

Treat that as the standard sync command unless a more local doc explicitly
overrides it.
After a coherent batch of changes in this workspace or one of its child components,
return to `ktools-csharp/` and run that sync command promptly.

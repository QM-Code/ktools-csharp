# C# Build Cleanup Project

## Mission

Make the C# workspace enforce clean build hygiene through `kbuild`, then make
sure the actual C# build/test/demo flow keeps generated artifacts under
`build/` instead of leaking into the source tree.

This task spans both `ktools-csharp/` and the sibling shared build repo
`../kbuild/`.

## Required Reading

- `../ktools/AGENTS.md`
- `AGENTS.md`
- `README.md`
- `../kbuild/AGENTS.md`
- `../kbuild/README.md`
- `../kbuild/libs/kbuild/residual_ops.py`
- `../kbuild/libs/kbuild/backend_ops.py`
- `../kbuild/libs/kbuild/csharp_backend.py`
- `../kbuild/tests/test_java_residuals.py`
- `kcli/AGENTS.md`
- `kcli/README.md`
- `ktrace/AGENTS.md`
- `ktrace/README.md`

## Current Gaps

- `kbuild` does not yet have a C# backend residual checker.
- The C# toolchain can easily create `obj/`, `bin/`, and related .NET outputs
  if any step escapes the staged `build/` layout.
- Both `kcli/` and `ktrace/` should be treated as part of the task, not just
  one repo.

## Work Plan

1. Add the C# residual checker in `kbuild`.
- Follow the existing Java residual-check pattern, but make it C#-appropriate.
- Detect generated .NET build output outside `build/`, for example stray
  `obj/`, `bin/`, test result output, or compiled runtime artifacts written
  into the source tree.
- Keep the checker narrow and defensible: target real build residuals, not
  arbitrary user files.

2. Add focused `kbuild` tests.
- Add tests similar in shape to `tests/test_java_residuals.py`.
- Cover both build refusal and `--git-sync` refusal when known C# build
  residuals appear outside `build/`.
- Cover the positive case where staged output inside `build/` is allowed.

3. Audit the actual C# build flow.
- Build `kcli/` and `ktrace/` through normal `kbuild` entrypoints.
- Identify whether any step writes C# build output outside `build/`.
- If so, fix the build flow so generated artifacts stay under `build/`.

4. Clean up real residuals if they exist.
- Remove any tracked or generated source-tree residuals that violate the new
- checker.
- Tighten ignore rules if needed so the same leak does not return.

5. Keep docs aligned.
- Update `kbuild` docs if the new checker needs backend-specific mention.
- Update local workspace/repo docs only if they currently encourage workflows
  that leak output outside `build/`.

## Constraints

- Do not weaken the strict `kbuild` hygiene model.
- Do not make the checker overly broad or speculative.
- Prefer fixing the build flow over just ignoring artifacts.

## Validation

- Run the new `kbuild` residual tests
- `cd ktools-csharp && kbuild --batch --build-latest`
- `cd ktools-csharp/kcli && kbuild --build-demos`
- `cd ktools-csharp/ktrace && kbuild --build-demos`
- Confirm the workspace stays clean with no generated C# artifacts outside
  `build/`

## Done When

- `kbuild` rejects known C# build residuals outside `build/`.
- The C# workspace no longer generates those residuals in normal use.
- Build and git-sync hygiene are enforced instead of relying on manual
  discipline.

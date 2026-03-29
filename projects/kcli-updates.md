# C# kcli Project

## Mission

Make `ktools-csharp/kcli/` clean, reviewable, and easy to compare against the
C++ reference while preserving the current C# API shape and .NET naming style.

## Required Reading

- `../ktools/AGENTS.md`
- `AGENTS.md`
- `kcli/AGENTS.md`
- `kcli/README.md`
- `../ktools-cpp/kcli/README.md`
- `../ktools-cpp/kcli/docs/behavior.md`
- `../ktools-cpp/kcli/cmake/tests/kcli_api_cases.cpp`

## Current Gaps

- A large amount of generated output is still tracked under `kcli/build/latest`
  and `kcli/demo/**/build/latest`.
- `kcli/README.md` still points readers at the old monolithic
  `ParseEngine.cs`/`Program.cs` layout instead of the current split source and
  test files.
- The parser internals are now split, but the repo still needs a deliberate
  parity audit against the full C++ contract.
- Demo layout and documentation should be rechecked as contract material, not
  just examples.

## Work Plan

1. Clean repo hygiene aggressively.
- Remove tracked build products from `build/latest` and demo build trees.
- Tighten ignore rules so generated output does not return.
- Make the hand-maintained source tree the dominant shape of the repo again.

2. Reconcile docs with the actual layout.
- Update `kcli/README.md` and any other local docs that still reference the old
  file structure.
- Point reviewers at the real current parser/test files.

3. Re-audit behavior parity with C++.
- Compare C# behavior against the C++ docs and test contract for help output,
  alias expansion, inline roots, root value handlers, optional/required value
  handling, double-dash rejection, and validation-before-handler execution.
- Add focused tests where a documented reference behavior is not yet asserted.

4. Review demo parity with the reference.
- Confirm that bootstrap, sdk, and executable demos still match the reference
  roles and naming.
- Tighten demo README files if they leave behavior implicit.

5. Apply only narrow structural polish.
- Keep the current split parser layout if it remains readable.
- Only rename or move files when that clearly improves navigability for
  reviewers and porters.

## Constraints

- Do not weaken the public API without a strong reason.
- Keep demo behavior aligned with the reference.
- Prefer targeted cleanup over another broad refactor.

## Validation

- `cd ktools-csharp/kcli && kbuild --build-latest`
- `cd ktools-csharp/kcli && kbuild --build-demos`
- `dotnet ktools-csharp/kcli/build/latest/tests/bin/Kcli.Tests.dll`
- Run the demo entrypoints listed in `ktools-csharp/kcli/README.md`

## Done When

- Generated build output no longer obscures the repo.
- README/docs point at the real current structure.
- Tests, demos, and docs together make C# easy to compare with C++.

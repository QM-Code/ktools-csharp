# C# kcli Project

## Mission

Bring `ktools-csharp/kcli/` structurally up to the level of the C++ reference
while preserving the current C# API shape and .NET naming style.

## Required Reading

- `../ktools/AGENTS.md`
- `AGENTS.md`
- `kcli/AGENTS.md`
- `kcli/README.md`
- `../ktools-cpp/kcli/README.md`
- `../ktools-cpp/kcli/docs/behavior.md`
- `../ktools-cpp/kcli/cmake/tests/kcli_api_cases.cpp`

## Current Gaps

- Core internals are too concentrated in `kcli/src/Kcli/ParseEngine.cs`.
- Multiple model and helper types are packed into
  `kcli/src/Kcli/InternalTypes.cs`.
- All test coverage is routed through one file:
  `kcli/tests/src/Kcli.Tests/Program.cs`.
- The repo contains tracked build output under `kcli/build/latest` and
  `kcli/demo/**/build/latest`, which makes the implementation harder to review.

## Work Plan

1. Refactor the core source layout.
- Split `InternalTypes.cs` into dedicated files for bindings, parse outcome,
  invocations, token classification, and help rows.
- Split `ParseEngine.cs` into smaller units if that can be done without making
  control flow harder to follow.
- Keep the public entrypoints in `Parser.cs` and `InlineParser.cs` clean and
  small, closer to the reference layout.

2. Improve test structure.
- Replace the single `Program.cs` test harness with multiple test files grouped
  by concern: API behavior, bootstrap/demo behavior, core demo behavior, omega
  demo behavior, and test support.
- Preserve current coverage while making failures easier to localize.
- Add missing tests where C++ behavior is documented but not asserted in C#.

3. Tighten repo hygiene.
- Remove tracked build products from source control if possible.
- If some staged outputs are intentionally versioned, document why and keep that
  policy narrow.
- Make the hand-maintained source tree stand out from generated outputs.

4. Confirm demo parity with C++.
- Check that bootstrap, sdk, and executable demo scenarios match the C++
  contract.
- Ensure the C# demo layout stays aligned with the reference naming and role of
  each demo.

5. Reconcile behavior details.
- Match C++ semantics for help, alias expansion, inline roots, required values,
  optional values, error text, and validation-before-handler execution.
- Preserve C#-idiomatic naming while keeping conceptual parity.

## Constraints

- Do not weaken the current public API without a strong reason.
- Keep demo behavior aligned with the reference.
- Prefer narrower files and clearer boundaries over introducing abstraction for
  its own sake.

## Validation

- `cd ktools-csharp/kcli && kbuild --build-latest`
- `cd ktools-csharp/kcli && kbuild --build-demos`
- `dotnet ktools-csharp/kcli/build/latest/tests/bin/Kcli.Tests.dll`
- Run the demo entrypoints listed in `ktools-csharp/kcli/README.md`

## Done When

- The core parser internals are easier to navigate than they are today.
- Tests are split by scenario instead of concentrated in one file.
- Repo noise from generated artifacts is substantially reduced.
- A reviewer can compare C# to C++ without fighting the repo layout.

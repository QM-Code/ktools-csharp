# C# ktrace Project

## Mission

Make `ktools-csharp/ktrace/` clean, parity-checked, and easy to compare
against the C++ reference while preserving the current C# API shape and .NET
naming style.

## Required Reading

- `../ktools/AGENTS.md`
- `AGENTS.md`
- `ktrace/AGENTS.md`
- `ktrace/README.md`
- `ktrace/docs/api.md`
- `ktrace/docs/behavior.md`
- `../ktools-cpp/ktrace/README.md`
- `../ktools-cpp/ktrace/include/ktrace.hpp`
- `../ktools-cpp/ktrace/src/ktrace/cli.cpp`
- `../ktools-cpp/ktrace/cmake/tests/ktrace_channel_semantics_test.cpp`
- `../ktools-cpp/ktrace/cmake/tests/ktrace_format_api_test.cpp`
- `../ktools-cpp/ktrace/cmake/tests/ktrace_log_api_test.cpp`

## Current Gaps

- A large amount of generated output is still tracked under `ktrace/build/latest`
  and `ktrace/demo/**/build/latest`.
- `ktrace/demo/common/` exists and is the wrong demo structure.
- The test surface is still concentrated in one `tests/src/Ktrace.Tests/Program.cs`
  file.
- The implementation needs a deliberate parity audit against the full C++
  contract for selectors, logger behavior, output formatting, and CLI
  integration.

## Work Plan

1. Clean repo hygiene aggressively.
- Remove tracked build products from `build/latest` and demo build trees.
- Tighten ignore rules so generated output does not return.
- Make the handwritten source tree the dominant shape of the repo again.

2. Eliminate shared demo code.
- Remove `ktrace/demo/common/`.
- Make `demo/sdk/alpha`, `demo/sdk/beta`, and `demo/sdk/gamma` self-contained.
- Keep bootstrap logic under `demo/bootstrap/`.
- Keep composition logic under `demo/exe/core/` and `demo/exe/omega/`.
- Do not replace `demo/common/` with another disguised shared demo layer.

3. Re-audit behavior parity with C++.
- Compare C# behavior against the C++ contract for channel registration,
  selector parsing, unmatched-selector warnings, logger/trace-source
  attachment, `traceChanged(...)`, output options, and the generated
  `--trace-*` surface.
- Add focused tests where a reference behavior is not asserted directly.

4. Improve test and doc discoverability where it helps.
- Split tests by concern if that makes failures easier to localize.
- Update local docs if they still hide important behavior or structure.

5. Apply only narrow structural polish.
- Keep the current library layout if it remains readable.
- Only rename or move files when that clearly improves navigability.

## Constraints

- Do not weaken the public API without a strong reason.
- Keep demo behavior aligned with the reference.
- Prefer targeted cleanup over another broad refactor.

## Validation

- `cd ktools-csharp/ktrace && kbuild --build-latest`
- `cd ktools-csharp/ktrace && kbuild --build-demos`
- `dotnet ktools-csharp/ktrace/build/latest/tests/bin/Ktrace.Tests.dll`
- Run the demo entrypoints listed in `ktools-csharp/ktrace/README.md`

## Done When

- Generated build output no longer obscures the repo.
- Shared demo code is gone.
- Tests, demos, and docs together make C# easy to compare with C++.

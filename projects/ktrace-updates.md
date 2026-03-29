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

- The repo is much cleaner now, but it still needs a deliberate parity audit
  against the full C++ contract for selectors, logger behavior, output
  formatting, and CLI integration.
- The split test suite should be checked for any remaining gaps in explicit
  bootstrap/core/omega demo-contract coverage.
- Local docs and examples should be rechecked so they point reviewers at the
  current structure and behavior directly.
- Only narrow structural polish should remain; any further refactor has to
  justify itself clearly.

## Work Plan

1. Re-audit behavior parity with C++.
- Compare C# behavior against the C++ contract for channel registration,
  selector parsing, unmatched-selector warnings, logger/trace-source
  attachment, `traceChanged(...)`, output options, and the generated
  `--trace-*` surface.
- Add focused tests where a reference behavior is not asserted directly.

2. Tighten explicit contract coverage.
- Review whether bootstrap/core/omega demo behavior is asserted directly enough
  rather than only through general API/CLI tests.
- Add focused coverage where the current tests still leave demo behavior
  implicit.

3. Reconcile docs with the actual repo shape.
- Update `ktrace/README.md`, docs pages, and examples if they still assume more
  reader inference than necessary.
- Keep reviewers pointed at the real current source and test files.

4. Apply only narrow structural polish.
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

- Tests, demos, and docs together make C# easy to compare with C++.
- Demo-contract coverage is explicit rather than assumed.
- Any further structural change is clearly justified by readability.

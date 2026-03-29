# C# Updates

## Mission

Keep `ktools-csharp/` easy to compare with the C++ reference while preserving
the current C# API shape and .NET naming style across both `kcli` and
`ktrace`.

## Required Reading

- `../ktools/AGENTS.md`
- `AGENTS.md`
- `README.md`
- `kcli/AGENTS.md`
- `kcli/README.md`
- `ktrace/AGENTS.md`
- `ktrace/README.md`
- `ktrace/docs/api.md`
- `ktrace/docs/behavior.md`
- `../ktools-cpp/kcli/README.md`
- `../ktools-cpp/kcli/docs/behavior.md`
- `../ktools-cpp/kcli/cmake/tests/kcli_api_cases.cpp`
- `../ktools-cpp/ktrace/README.md`
- `../ktools-cpp/ktrace/include/ktrace.hpp`
- `../ktools-cpp/ktrace/src/ktrace/cli.cpp`
- `../ktools-cpp/ktrace/cmake/tests/ktrace_channel_semantics_test.cpp`
- `../ktools-cpp/ktrace/cmake/tests/ktrace_format_api_test.cpp`
- `../ktools-cpp/ktrace/cmake/tests/ktrace_log_api_test.cpp`

## kcli Focus

- Re-audit parser behavior against the full C++ contract for help output,
  alias expansion, inline roots, root value handlers, optional and required
  values, double-dash rejection, and validation-before-handler execution.
- Update local docs if they still point reviewers at stale layout assumptions
  instead of the current split parser and test files.
- Keep bootstrap, SDK, and executable demos explicit as contract material, not
  just examples.

## ktrace Focus

- Re-audit selector behavior, logger and trace-source attachment,
  `traceChanged(...)`, output formatting, and generated `--trace-*` CLI
  behavior against the C++ contract.
- Make demo-contract coverage explicit enough that bootstrap, core, and omega
  behavior is not only inferred from generic tests.
- Tighten docs and examples where the current structure or behavior still
  requires reader inference.

## Cross-Cutting Rules

- Do not introduce a shared demo support layer.
- Prefer narrow cleanup and focused tests over another broad structural pass.
- Preserve the public C# API unless a change is clearly justified.

## Validation

- `cd ktools-csharp/kcli && kbuild --build-latest`
- `cd ktools-csharp/kcli && kbuild --build-demos`
- `dotnet ktools-csharp/kcli/build/latest/tests/bin/Kcli.Tests.dll`
- `cd ktools-csharp/ktrace && kbuild --build-latest`
- `cd ktools-csharp/ktrace && kbuild --build-demos`
- `dotnet ktools-csharp/ktrace/build/latest/tests/bin/Ktrace.Tests.dll`
- Run the demo entrypoints listed in each repo README

## Done When

- `kcli` and `ktrace` are both easy to compare with the C++ reference.
- Demo behavior is explicit and self-contained.
- Docs, tests, and source layout all point at the same current structure.

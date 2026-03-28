# `kbuild` In `ktools-csharp`

The C# workspace currently uses the shared `kbuild` command as its build
orchestration entrypoint.

## Current Status

- the checked-out workspace does not currently contain a separate `kbuild/`
  implementation directory
- the documented workflow assumes `kbuild` is available on `PATH`
- the workspace-level build story is still expected to align with the shared
  `kbuild` command model

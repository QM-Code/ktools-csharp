# Examples

This page shows a few common `kcli` patterns. For complete compiling examples,
also see:

- [`../demo/sdk/alpha/src/Kcli/Demo/Alpha/AlphaSdk.cs`](../demo/sdk/alpha/src/Kcli/Demo/Alpha/AlphaSdk.cs)
- [`../demo/sdk/beta/src/Kcli/Demo/Beta/BetaSdk.cs`](../demo/sdk/beta/src/Kcli/Demo/Beta/BetaSdk.cs)
- [`../demo/sdk/gamma/src/Kcli/Demo/Gamma/GammaSdk.cs`](../demo/sdk/gamma/src/Kcli/Demo/Gamma/GammaSdk.cs)
- [`../demo/exe/core/src/Kcli/Demo/Core/Program.cs`](../demo/exe/core/src/Kcli/Demo/Core/Program.cs)
- [`../demo/exe/omega/src/Kcli/Demo/Omega/Program.cs`](../demo/exe/omega/src/Kcli/Demo/Omega/Program.cs)

## Minimal Executable

```csharp
using Kcli;

Parser parser = new Parser();

parser.AddAlias("-v", "--verbose");
parser.SetHandler("--verbose", context => { }, "Enable verbose logging.");

parser.ParseOrExit(args);
```

## Inline Root With Subcommands-Like Options

```csharp
Parser parser = new Parser();
InlineParser build = new InlineParser("--build");

build.SetHandler("-profile", (context, value) => { }, "Set build profile.");
build.SetHandler("-clean", context => { }, "Enable clean build.");

parser.AddInlineParser(build);
parser.ParseOrExit(args);
```

This enables:

```text
--build
--build-profile release
--build-clean
```

## Bare Root Value Handler

```csharp
InlineParser config = new InlineParser("--config");

config.SetRootValueHandler(
    (context, value) => { },
    "<assignment>",
    "Store a config assignment.");
```

This enables:

```text
--config
--config user=alice
```

Behavior:

- `--config` prints inline help
- `--config user=alice` invokes the root value handler

## Alias Preset Tokens

```csharp
Parser parser = new Parser();

parser.AddAlias("-c", "--config-load", "user-file");
parser.SetHandler("--config-load", (context, value) => { }, "Load config.");
```

This makes:

```text
-c settings.json
```

behave like:

```text
--config-load user-file settings.json
```

Inside the handler:

- `context.Option` is `--config-load`
- `context.ValueTokens` is `["user-file", "settings.json"]`

## Optional Values

```csharp
parser.SetOptionalValueHandler(
    "--color",
    (context, value) => { },
    "Set or auto-detect color output.");
```

This enables both:

```text
--color
--color always
```

## Positionals

```csharp
parser.SetPositionalHandler(context =>
{
    foreach (string token in context.ValueTokens)
    {
        UsePositional(token);
    }
});
```

The positional handler receives all remaining non-option tokens after option
parsing succeeds.

## Custom Error Handling

If you want your own formatting or exit policy, use `ParseOrThrow()`:

```csharp
try
{
    parser.ParseOrThrow(args);
}
catch (CliError ex)
{
    Console.Error.WriteLine($"custom cli error: {ex.Message}");
    return;
}
```

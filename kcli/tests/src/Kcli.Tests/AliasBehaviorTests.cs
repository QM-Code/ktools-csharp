using System;
using System.Collections.Generic;
using Ktools.CSharp.Tests;

namespace Kcli.Tests;

internal static class AliasBehaviorTests
{
    public static void Run()
    {
        TestAliasRewritesOption();
        TestAliasPresetTokensAppendToValueHandlers();
        TestAliasPresetTokensSatisfyRequiredValue();
        TestAliasPresetTokensApplyToInlineRootValue();
        TestAliasPresetTokensRejectedForFlags();
        TestAliasDoesNotRewriteConsumedValueTokens();
        TestAliasAfterDoubleDashStillFailsValidationBeforeHandlersRun();
        TestAliasRejectsInvalidAlias();
        TestAliasRejectsTargetWithWhitespace();
        TestAliasRejectsInvalidTarget();
        TestAliasRejectsSingleDashTarget();
    }

    private static void TestAliasRewritesOption()
    {
        string seen = string.Empty;
        Parser parser = new Parser();
        parser.AddAlias("-v", "--verbose");
        parser.SetHandler("--verbose", context => seen = context.Option, "Enable verbose logging.");
        parser.ParseOrThrow(new[] { "-v" });
        TestAssert.Equal(seen, "--verbose", "alias should rewrite the effective option");
    }

    private static void TestAliasPresetTokensAppendToValueHandlers()
    {
        string joined = string.Empty;
        List<string> tokens = new List<string>();
        Parser parser = new Parser();
        parser.AddAlias("-c", "--config-load", "user-file");
        parser.SetHandler("--config-load", (context, value) =>
        {
            joined = value;
            tokens.AddRange(context.ValueTokens);
        }, "Load config.");

        parser.ParseOrThrow(new[] { "-c", "settings.json" });

        TestAssert.Equal(joined, "user-file settings.json", "preset tokens should be prepended to explicit values");
        TestAssert.Equal(string.Join("|", tokens), "user-file|settings.json", "context tokens should include preset and explicit values");
    }

    private static void TestAliasPresetTokensSatisfyRequiredValue()
    {
        string seen = string.Empty;
        List<string> tokens = new List<string>();
        Parser parser = new Parser();
        parser.AddAlias("-p", "--profile", "release");
        parser.SetHandler("--profile", (context, value) =>
        {
            seen = value;
            tokens.AddRange(context.ValueTokens);
        }, "Set active profile.");
        parser.ParseOrThrow(new[] { "-p" });
        TestAssert.Equal(seen, "release", "preset token should satisfy required value");
        TestAssert.Equal(string.Join("|", tokens), "release", "context tokens should include preset value");
    }

    private static void TestAliasPresetTokensApplyToInlineRootValue()
    {
        string option = string.Empty;
        string value = string.Empty;

        Parser parser = new Parser();
        InlineParser config = new InlineParser("--config");
        config.SetRootValueHandler((context, captured) =>
        {
            option = context.Option;
            value = captured;
        }, "<assignment>", "Store a config assignment.");
        parser.AddInlineParser(config);
        parser.AddAlias("-c", "--config", "user-file=/tmp/user.json");
        parser.ParseOrThrow(new[] { "-c" });

        TestAssert.Equal(option, "--config", "inline root option should be preserved");
        TestAssert.Equal(value, "user-file=/tmp/user.json", "preset value should reach root handler");
    }

    private static void TestAliasPresetTokensRejectedForFlags()
    {
        Parser parser = new Parser();
        parser.AddAlias("-v", "--verbose", "unexpected");
        parser.SetHandler("--verbose", _ => { }, "Enable verbose logging.");

        CliError error = TestAssert.Throws<CliError>(() => parser.ParseOrThrow(new[] { "-v" }), "flag aliases must reject preset tokens");
        TestAssert.Equal(error.Option, "-v", "error should surface the alias token");
        TestAssert.Contains(error.Message, "does not accept values", "error should explain the flag rejection");
    }

    private static void TestAliasDoesNotRewriteConsumedValueTokens()
    {
        string captured = string.Empty;
        Parser parser = new Parser();
        parser.AddAlias("-v", "--verbose");
        parser.SetHandler("--output", (_, value) => captured = value, "Set output target.");
        parser.SetHandler("--verbose", _ => throw new InvalidOperationException("value tokens should not be alias-expanded"), "Enable verbose logging.");

        parser.ParseOrThrow(new[] { "--output", "-v" });

        TestAssert.Equal(captured, "-v", "required values should preserve alias-shaped tokens verbatim");
    }

    private static void TestAliasAfterDoubleDashStillFailsValidationBeforeHandlersRun()
    {
        bool verbose = false;
        Parser parser = new Parser();
        parser.AddAlias("-v", "--verbose");
        parser.SetHandler("--verbose", _ => verbose = true, "Enable verbose logging.");

        CliError error = TestAssert.Throws<CliError>(
            () => parser.ParseOrThrow(new[] { "--", "-v" }),
            "double dash should remain invalid even when later tokens are recognized");

        TestAssert.True(!verbose, "handlers should not run when the full command line is invalid");
        TestAssert.Equal(error.Option, "--", "double dash should be reported as the failing token");
    }

    private static void TestAliasRejectsInvalidAlias()
    {
        Parser parser = new Parser();
        ArgumentException error = TestAssert.Throws<ArgumentException>(
            () => parser.AddAlias("--verbose", "--verbose"),
            "aliases must use single-dash form");
        TestAssert.Contains(error.Message, "single-dash form", "alias normalization errors should describe the accepted shape");
    }

    private static void TestAliasRejectsInvalidTarget()
    {
        Parser parser = new Parser();
        ArgumentException error = TestAssert.Throws<ArgumentException>(
            () => parser.AddAlias("-v", "verbose"),
            "aliases should reject targets without a double dash");
        TestAssert.Contains(error.Message, "double-dash form", "alias target normalization errors should describe the accepted shape");
    }

    private static void TestAliasRejectsTargetWithWhitespace()
    {
        Parser parser = new Parser();
        ArgumentException error = TestAssert.Throws<ArgumentException>(
            () => parser.AddAlias("-v", "--bad target"),
            "aliases should reject targets with whitespace");
        TestAssert.Contains(error.Message, "double-dash form", "whitespace alias targets should surface the standard target error");
    }

    private static void TestAliasRejectsSingleDashTarget()
    {
        Parser parser = new Parser();
        ArgumentException error = TestAssert.Throws<ArgumentException>(
            () => parser.AddAlias("-v", "-verbose"),
            "aliases should reject single-dash targets");
        TestAssert.Contains(error.Message, "double-dash form", "single-dash alias targets should still surface the standard target error");
    }
}

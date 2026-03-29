using System.Collections.Generic;
using Ktools.CSharp.Tests;

namespace Kcli.Tests;

internal static class ValueHandlingTests
{
    public static void Run()
    {
        TestRequiredValueAcceptsOptionLikeFirstToken();
        TestRequiredValueRejectsMissingValue();
        TestRequiredValuePreservesShellWhitespace();
        TestOptionalValueHandlerAllowsMissingValue();
        TestOptionalValueHandlerAcceptsExplicitEmptyValue();
        TestFlagHandlerDoesNotConsumeFollowingTokens();
        TestPositionalHandlerPreservesExplicitEmptyTokens();
        TestSinglePassProcessingConsumesInlineOptionsAndPositionals();
    }

    private static void TestRequiredValueAcceptsOptionLikeFirstToken()
    {
        string captured = string.Empty;
        Parser parser = new Parser();
        parser.AddAlias("-v", "--verbose");
        parser.SetHandler("--output", (_, value) => captured = value, "Set output target.");
        parser.SetHandler("--verbose", _ => throw new System.InvalidOperationException("verbose should not be treated as a separate option"), "Enable verbose logging.");
        parser.ParseOrThrow(new[] { "--output", "-v" });
        TestAssert.Equal(captured, "-v", "required values should accept option-like first tokens");
    }

    private static void TestRequiredValueRejectsMissingValue()
    {
        Parser parser = new Parser();
        parser.SetHandler("--output", (_, _) => { }, "Set output target.");

        CliError error = TestAssert.Throws<CliError>(
            () => parser.ParseOrThrow(new[] { "--output" }),
            "required-value handlers should reject missing values");

        TestAssert.Equal(error.Option, "--output", "missing required values should report the failing option");
        TestAssert.Contains(error.Message, "requires a value", "missing required values should describe the contract");
    }

    private static void TestRequiredValuePreservesShellWhitespace()
    {
        string joined = string.Empty;
        List<string> tokens = new List<string>();
        Parser parser = new Parser();
        parser.SetHandler("--name", (context, value) =>
        {
            joined = value;
            tokens.AddRange(context.ValueTokens);
        }, "Set the display name.");

        parser.ParseOrThrow(new[] { "--name", "Joe", string.Empty, "Smith" });

        TestAssert.Equal(joined, "Joe  Smith", "joined values should preserve explicit empty tokens");
        TestAssert.Equal(string.Join("|", tokens), "Joe||Smith", "value tokens should preserve explicit empties");
    }

    private static void TestOptionalValueHandlerAllowsMissingValue()
    {
        string captured = "unset";
        int tokenCount = -1;
        Parser parser = new Parser();
        parser.SetOptionalValueHandler("--color", (context, value) =>
        {
            captured = value;
            tokenCount = context.ValueTokens.Count;
        }, "Set or auto-detect color output.");

        parser.ParseOrThrow(new[] { "--color" });

        TestAssert.Equal(captured, string.Empty, "optional value handlers should receive an empty string when no value is provided");
        TestAssert.Equal(tokenCount, 0, "optional value handlers should not synthesize tokens when no value is provided");
    }

    private static void TestOptionalValueHandlerAcceptsExplicitEmptyValue()
    {
        string captured = "unset";
        List<string> tokens = new List<string>();

        Parser parser = new Parser();
        parser.SetOptionalValueHandler("--color", (context, value) =>
        {
            captured = value;
            tokens.AddRange(context.ValueTokens);
        }, "Set or auto-detect color output.");
        parser.ParseOrThrow(new[] { "--color", string.Empty });

        TestAssert.Equal(captured, string.Empty, "optional value handlers should accept explicit empty values");
        TestAssert.Equal(tokens.Count, 1, "explicit empty values should still count as a value token");
        TestAssert.Equal(tokens[0], string.Empty, "explicit empty tokens should be preserved");
    }

    private static void TestFlagHandlerDoesNotConsumeFollowingTokens()
    {
        bool verbose = false;
        List<string> positionals = new List<string>();
        Parser parser = new Parser();
        parser.SetHandler("--verbose", _ => verbose = true, "Enable verbose logging.");
        parser.SetPositionalHandler(context => positionals.AddRange(context.ValueTokens));

        parser.ParseOrThrow(new[] { "--verbose", "input.txt" });

        TestAssert.True(verbose, "flag handlers should run when selected");
        TestAssert.Equal(string.Join("|", positionals), "input.txt", "flag handlers should not consume following positional tokens");
    }

    private static void TestPositionalHandlerPreservesExplicitEmptyTokens()
    {
        List<string> tokens = new List<string>();
        Parser parser = new Parser();
        parser.SetPositionalHandler(context => tokens.AddRange(context.ValueTokens));
        parser.ParseOrThrow(new[] { "first", string.Empty, "last" });

        TestAssert.Equal(string.Join("|", tokens), "first||last", "positional handlers should preserve explicit empty tokens");
    }

    private static void TestSinglePassProcessingConsumesInlineOptionsAndPositionals()
    {
        List<string> seen = new List<string>();
        Parser parser = new Parser();
        InlineParser build = new InlineParser("--build");
        build.SetHandler("-profile", (_, value) => seen.Add($"inline:{value}"), "Set build profile.");
        parser.AddInlineParser(build);
        parser.SetHandler("--verbose", _ => seen.Add("flag:--verbose"), "Enable verbose logging.");
        parser.SetPositionalHandler(context => seen.Add($"positionals:{string.Join("|", context.ValueTokens)}"));

        parser.ParseOrThrow(new[] { "--build-profile", "release", "--verbose", "file1", "file2" });

        TestAssert.Equal(
            string.Join(",", seen),
            "inline:release,flag:--verbose,positionals:file1|file2",
            "inline options, top-level options, and positionals should all be scheduled in one parse and executed in order");
    }
}

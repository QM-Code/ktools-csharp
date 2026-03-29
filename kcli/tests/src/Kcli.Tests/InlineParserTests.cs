using System;
using Ktools.CSharp.Tests;

namespace Kcli.Tests;

internal static class InlineParserTests
{
    public static void Run()
    {
        TestInlineParserRejectsInvalidRoot();
        TestInlineHandlerNormalizationAcceptsShortAndFullForms();
        TestInlineHandlerNormalizationRejectsWrongRoot();
        TestBareInlineRootPrintsHelp();
        TestRootValueHandlerHelpRowPrints();
        TestInlineRootValueHandlerJoinsTokens();
        TestInlineMissingRootValueHandlerErrors();
        TestUnknownInlineOptionErrors();
    }

    private static void TestInlineParserRejectsInvalidRoot()
    {
        ArgumentException error = TestAssert.Throws<ArgumentException>(
            () => new InlineParser("-build"),
            "inline parsers should reject single-dash roots");
        TestAssert.Contains(error.Message, "must use '--root' or 'root'", "inline root normalization errors should describe the accepted forms");
    }

    private static void TestInlineHandlerNormalizationAcceptsShortAndFullForms()
    {
        string seen = string.Empty;
        Parser parser = new Parser();
        InlineParser beta = new InlineParser("--beta");
        beta.SetHandler("--beta-enable", context => seen = context.Option, "Enable beta mode.");
        parser.AddInlineParser(beta);

        parser.ParseOrThrow(new[] { "--beta-enable" });

        TestAssert.Equal(seen, "--beta-enable", "full-form inline handler registration should preserve the effective option");
    }

    private static void TestInlineHandlerNormalizationRejectsWrongRoot()
    {
        InlineParser beta = new InlineParser("--beta");
        ArgumentException error = TestAssert.Throws<ArgumentException>(
            () => beta.SetHandler("--alpha-enable", _ => { }, "Enable alpha mode."),
            "inline handlers should reject mismatched full-form roots");
        TestAssert.Contains(error.Message, "--beta-name", "inline normalization errors should mention the owning root");
    }

    private static void TestBareInlineRootPrintsHelp()
    {
        Parser parser = new Parser();
        InlineParser alpha = new InlineParser("--alpha");
        alpha.SetOptionalValueHandler("-enable", (_, _) => { }, "Enable alpha processing.");
        parser.AddInlineParser(alpha);

        string stdout = TestConsole.CaptureStdout(() => parser.ParseOrThrow(new[] { "--alpha" }));
        TestAssert.Contains(stdout, "Available --alpha-* options:", "bare inline root should print help");
        TestAssert.Contains(stdout, "--alpha-enable [value]", "help should include optional value syntax");
    }

    private static void TestRootValueHandlerHelpRowPrints()
    {
        Parser parser = new Parser();
        InlineParser build = new InlineParser("--build");
        build.SetRootValueHandler((_, _) => { }, "<selector>", "Select build targets.");
        parser.AddInlineParser(build);

        string stdout = TestConsole.CaptureStdout(() => parser.ParseOrThrow(new[] { "--build" }));
        TestAssert.Contains(stdout, "--build <selector>", "bare root help should include the root value placeholder");
        TestAssert.Contains(stdout, "Select build targets.", "bare root help should include the root value description");
    }

    private static void TestInlineRootValueHandlerJoinsTokens()
    {
        string joined = string.Empty;

        Parser parser = new Parser();
        InlineParser config = new InlineParser("--config");
        config.SetRootValueHandler((context, value) =>
        {
            joined = value;
            TestAssert.Equal(string.Join("|", context.ValueTokens), "user=alice|profile=prod", "inline root handlers should receive all value tokens");
        }, "<assignment>", "Store a config assignment.");
        parser.AddInlineParser(config);
        parser.ParseOrThrow(new[] { "--config", "user=alice", "profile=prod" });

        TestAssert.Equal(joined, "user=alice profile=prod", "inline root values should be joined with spaces");
    }

    private static void TestInlineMissingRootValueHandlerErrors()
    {
        Parser parser = new Parser();
        parser.AddInlineParser(new InlineParser("--build"));

        CliError error = TestAssert.Throws<CliError>(
            () => parser.ParseOrThrow(new[] { "--build", "release" }),
            "bare roots with values should fail when no root value handler exists");

        TestAssert.Equal(error.Option, "--build", "missing root value handlers should report the bare root token");
        TestAssert.Contains(error.Message, "unknown value for option '--build'", "missing root value handlers should explain the failure");
    }

    private static void TestUnknownInlineOptionErrors()
    {
        Parser parser = new Parser();
        InlineParser alpha = new InlineParser("--alpha");
        alpha.SetHandler("-enable", _ => { }, "Enable alpha processing.");
        parser.AddInlineParser(alpha);

        CliError error = TestAssert.Throws<CliError>(
            () => parser.ParseOrThrow(new[] { "--alpha-unknown" }),
            "unknown inline options should surface as cli errors");

        TestAssert.Equal(error.Option, "--alpha-unknown", "unknown inline options should report the original token");
        TestAssert.Contains(error.Message, "unknown option --alpha-unknown", "unknown inline options should use the standard unknown-option text");
    }
}

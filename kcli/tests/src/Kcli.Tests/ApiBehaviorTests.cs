using System;
using System.Collections.Generic;
using Ktools.CSharp.Tests;

namespace Kcli.Tests;

internal static class ApiBehaviorTests
{
    public static void Run()
    {
        TestParserEmptyParseSucceeds();
        TestParserCanBeReusedAcrossParses();
        TestDuplicateInlineRootRejected();
        TestInlineParserSetRootRewritesEffectiveOption();
        TestPositionalHandlerRequiresNonEmpty();
        TestPrimaryHandlerRejectsSingleDashOption();
    }

    private static void TestParserEmptyParseSucceeds()
    {
        string[] argv = { "prog" };
        Parser parser = new Parser();
        parser.ParseOrThrow(argv.Length, argv);
        TestAssert.Equal(string.Join("|", argv), "prog", "parseOrThrow should leave argv unchanged");
    }

    private static void TestParserCanBeReusedAcrossParses()
    {
        List<string> outputs = new List<string>();
        Parser parser = new Parser();
        parser.SetHandler("--output", (_, value) => outputs.Add(value), "Set output target.");

        parser.ParseOrThrow(new[] { "--output", "stdout" });
        parser.ParseOrThrow(new[] { "--output", "stderr" });

        TestAssert.Equal(string.Join("|", outputs), "stdout|stderr", "parser instances should be reusable across parses");
    }

    private static void TestDuplicateInlineRootRejected()
    {
        Parser parser = new Parser();
        parser.AddInlineParser(new InlineParser("--build"));

        ArgumentException error = TestAssert.Throws<ArgumentException>(() =>
        {
            parser.AddInlineParser(new InlineParser("build"));
        }, "duplicate inline parser roots should be rejected");

        TestAssert.Contains(error.Message, "already registered", "duplicate root errors should explain the conflict");
    }

    private static void TestInlineParserSetRootRewritesEffectiveOption()
    {
        string seenRoot = string.Empty;
        string seenOption = string.Empty;

        Parser parser = new Parser();
        InlineParser inlineParser = new InlineParser("--alpha");
        inlineParser.SetHandler("-enable", context =>
        {
            seenRoot = context.Root;
            seenOption = context.Option;
        }, "Enable the renamed root.");
        inlineParser.SetRoot("--omega");
        parser.AddInlineParser(inlineParser);

        parser.ParseOrThrow(new[] { "--omega-enable" });

        TestAssert.Equal(seenRoot, "omega", "SetRoot should rewrite the effective handler root");
        TestAssert.Equal(seenOption, "--omega-enable", "SetRoot should rewrite the effective handler option");
    }

    private static void TestPositionalHandlerRequiresNonEmpty()
    {
        Parser parser = new Parser();
        ArgumentNullException error = TestAssert.Throws<ArgumentNullException>(
            () => parser.SetPositionalHandler(null),
            "positional handlers must not accept null");
        TestAssert.Equal(error.ParamName, "handler", "null positional handlers should surface the handler parameter name");
    }

    private static void TestPrimaryHandlerRejectsSingleDashOption()
    {
        Parser parser = new Parser();
        ArgumentException error = TestAssert.Throws<ArgumentException>(
            () => parser.SetHandler("-verbose", _ => { }, "Enable verbose logging."),
            "top-level handlers should reject single-dash options");
        TestAssert.Contains(error.Message, "must use '--name' or 'name'", "top-level normalization errors should explain the accepted forms");
    }
}

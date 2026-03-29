using System.Collections.Generic;
using Ktools.CSharp.Tests;

namespace Kcli.Tests;

internal static class CoreDemoTests
{
    public static void Run()
    {
        TestCoreDemoAcceptsOutputAlias();
        TestCoreDemoAcceptsAlphaAlias();
        TestCoreDemoBareAlphaPrintsHelp();
    }

    private static void TestCoreDemoAcceptsOutputAlias()
    {
        string output = string.Empty;
        Parser parser = DemoParserFactory.CreateCoreParser(_ => { }, (_, value) => output = value, (_, _) => { });

        parser.ParseOrThrow(new[] { "-out", "stdout" });

        TestAssert.Equal(output, "stdout", "core demo should expose the output alias");
    }

    private static void TestCoreDemoAcceptsAlphaAlias()
    {
        string option = string.Empty;
        List<string> tokens = new List<string>();
        Parser parser = DemoParserFactory.CreateCoreParser(
            _ => { },
            (_, _) => { },
            (context, value) =>
            {
                option = context.Option;
                tokens.AddRange(context.ValueTokens);
            });

        parser.ParseOrThrow(new[] { "-a" });

        TestAssert.Equal(option, "--alpha-enable", "core demo should expose the alpha alias");
        TestAssert.Equal(tokens.Count, 0, "core demo alpha enable should remain optional");
    }

    private static void TestCoreDemoBareAlphaPrintsHelp()
    {
        Parser parser = DemoParserFactory.CreateCoreParser(_ => { }, (_, _) => { }, (_, _) => { });

        string stdout = TestConsole.CaptureStdout(() => parser.ParseOrThrow(new[] { "--alpha" }));

        TestAssert.Contains(stdout, "Available --alpha-* options:", "core demo should expose alpha inline help");
        TestAssert.Contains(stdout, "--alpha-message <value>", "core demo help should list alpha message");
    }
}

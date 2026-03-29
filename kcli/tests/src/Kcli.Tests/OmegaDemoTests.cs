using System.Collections.Generic;
using Ktools.CSharp.Tests;

namespace Kcli.Tests;

internal static class OmegaDemoTests
{
    public static void Run()
    {
        TestOmegaDemoAcceptsBuildAlias();
        TestOmegaDemoSupportsRenamedGammaRoot();
        TestOmegaDemoBuildRootPrintsHelp();
        TestOmegaDemoCollectsPositionals();
    }

    private static void TestOmegaDemoAcceptsBuildAlias()
    {
        string option = string.Empty;
        string value = string.Empty;
        Parser parser = DemoParserFactory.CreateOmegaParser(
            _ => { },
            (_, _) => { },
            (_, _) => { },
            (_, _) => { },
            (_, _) => { },
            (context, captured) =>
            {
                option = context.Option;
                value = captured;
            },
            _ => { });

        parser.ParseOrThrow(new[] { "-b", "release" });

        TestAssert.Equal(option, "--build-profile", "omega demo should expose the build alias");
        TestAssert.Equal(value, "release", "omega demo build alias should preserve the supplied value");
    }

    private static void TestOmegaDemoSupportsRenamedGammaRoot()
    {
        string option = string.Empty;
        string root = string.Empty;
        Parser parser = DemoParserFactory.CreateOmegaParser(
            _ => { },
            (_, _) => { },
            (_, _) => { },
            (_, _) => { },
            (context, _) =>
            {
                option = context.Option;
                root = context.Root;
            },
            (_, _) => { },
            _ => { });

        parser.ParseOrThrow(new[] { "--newgamma-tag", "prod" });

        TestAssert.Equal(option, "--newgamma-tag", "omega demo should use the overridden gamma root in effective options");
        TestAssert.Equal(root, "newgamma", "omega demo should report the overridden gamma root");
    }

    private static void TestOmegaDemoBuildRootPrintsHelp()
    {
        Parser parser = DemoParserFactory.CreateOmegaParser(
            _ => { },
            (_, _) => { },
            (_, _) => { },
            (_, _) => { },
            (_, _) => { },
            (_, _) => { },
            _ => { });

        string stdout = TestConsole.CaptureStdout(() => parser.ParseOrThrow(new[] { "--build" }));

        TestAssert.Contains(stdout, "Available --build-* options:", "omega demo should expose build inline help");
        TestAssert.Contains(stdout, "--build-profile <value>", "omega demo help should list the build profile option");
    }

    private static void TestOmegaDemoCollectsPositionals()
    {
        List<string> positionals = new List<string>();
        Parser parser = DemoParserFactory.CreateOmegaParser(
            _ => { },
            (_, _) => { },
            (_, _) => { },
            (_, _) => { },
            (_, _) => { },
            (_, _) => { },
            context => positionals.AddRange(context.ValueTokens));

        parser.ParseOrThrow(new[] { "first", "second" });

        TestAssert.Equal(string.Join("|", positionals), "first|second", "omega demo should keep positional handling enabled");
    }
}

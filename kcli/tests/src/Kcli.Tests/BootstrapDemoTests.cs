using Ktools.CSharp.Tests;

namespace Kcli.Tests;

internal static class BootstrapDemoTests
{
    public static void Run()
    {
        TestBootstrapAliasRoutesToVerboseHandler();
    }

    private static void TestBootstrapAliasRoutesToVerboseHandler()
    {
        string option = string.Empty;
        Parser parser = DemoParserFactory.CreateBootstrapParser(context => option = context.Option);

        parser.ParseOrThrow(new[] { "-v" });

        TestAssert.Equal(option, "--verbose", "bootstrap demo should expose the verbose alias");
    }
}

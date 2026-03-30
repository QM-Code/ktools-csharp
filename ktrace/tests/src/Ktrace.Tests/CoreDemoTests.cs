using Kcli;
using Ktools.CSharp.Tests;

namespace Ktrace.Tests;

internal static class CoreDemoTests
{
    public static void Run()
    {
        TestCoreDemoResolvesLocalSelectorsAgainstTheExecutableNamespace();
        TestCoreDemoCanEnableImportedAlphaChannels();
    }

    private static void TestCoreDemoResolvesLocalSelectorsAgainstTheExecutableNamespace()
    {
        Parser parser = DemoLoggerFactory.CreateCoreParser(out Logger logger, out TraceLogger trace);

        parser.ParseOrThrow(new[] { "--trace", ".app" });

        TestAssert.True(logger.ShouldTraceChannel(trace, ".app"), "core demo should resolve leading-dot selectors against the local trace namespace");
    }

    private static void TestCoreDemoCanEnableImportedAlphaChannels()
    {
        Parser parser = DemoLoggerFactory.CreateCoreParser(out Logger logger, out _);

        parser.ParseOrThrow(new[] { "--trace", "alpha.net" });

        TestAssert.True(logger.ShouldTraceChannel("alpha.net"), "core demo should expose imported alpha channels through the trace CLI");
    }
}

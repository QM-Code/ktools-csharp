using Kcli;
using Ktools.CSharp.Tests;

namespace Ktrace.Tests;

internal static class OmegaDemoTests
{
    public static void Run()
    {
        TestOmegaDemoSupportsDepthThreeWildcardSelection();
        TestOmegaDemoSupportsBraceSelectorsAcrossImportedNamespaces();
        TestOmegaDemoTraceFunctionsEnablesSharedSourceFormatting();
    }

    private static void TestOmegaDemoSupportsDepthThreeWildcardSelection()
    {
        Parser parser = DemoLoggerFactory.CreateOmegaParser(out Logger logger, out _);

        parser.ParseOrThrow(new[] { "--trace", "*.*.*.*" });

        TestAssert.True(logger.ShouldTraceChannel("omega.deep.branch.leaf"), "omega demo should enable its deepest local channel through the wildcard examples");
        TestAssert.True(logger.ShouldTraceChannel("alpha.net.gamma.deep"), "omega demo should enable imported depth-three channels");
        TestAssert.True(logger.ShouldTraceChannel("beta.io"), "omega demo should keep imported top-level channels in the wildcard surface");
        TestAssert.True(logger.ShouldTraceChannel("gamma.metrics"), "omega demo should include imported namespaces in wildcard selection");
    }

    private static void TestOmegaDemoSupportsBraceSelectorsAcrossImportedNamespaces()
    {
        Parser parser = DemoLoggerFactory.CreateOmegaParser(out Logger logger, out _);

        parser.ParseOrThrow(new[] { "--trace", "*.{net,io}" });

        TestAssert.True(logger.ShouldTraceChannel("alpha.net"), "omega demo brace selectors should match imported alpha channels");
        TestAssert.True(logger.ShouldTraceChannel("beta.io"), "omega demo brace selectors should match imported beta channels");
        TestAssert.True(!logger.ShouldTraceChannel("alpha.cache"), "omega demo brace selectors should leave unmatched channels disabled");
        TestAssert.True(!logger.ShouldTraceChannel("gamma.metrics"), "omega demo brace selectors should not enable unrelated namespaces");
    }

    private static void TestOmegaDemoTraceFunctionsEnablesSharedSourceFormatting()
    {
        Parser parser = DemoLoggerFactory.CreateOmegaParser(out Logger logger, out _);

        parser.ParseOrThrow(new[] { "--trace-functions" });

        OutputOptions options = logger.GetOutputOptions();
        TestAssert.True(options.Filenames, "omega demo should expose trace file output toggles");
        TestAssert.True(options.LineNumbers, "omega demo trace-functions should enable line numbers");
        TestAssert.True(options.FunctionNames, "omega demo trace-functions should enable function names");
    }
}

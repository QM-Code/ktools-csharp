using Kcli;
using Ktools.CSharp.Tests;

namespace Ktrace.Tests;

internal static class CliTests
{
    public static void Run()
    {
        VerifyInlineParserSurface();
    }

    private static void VerifyInlineParserSurface()
    {
        Logger logger = new Logger();
        TraceLogger trace = new TraceLogger("tests");
        trace.AddChannel("app", "HotPink");
        logger.AddTraceLogger(trace);

        Parser parser = new Parser();
        parser.AddInlineParser(logger.MakeInlineParser(trace));

        string help = TestConsole.CaptureStdout(() => parser.ParseOrThrow(new[] { "--trace" }));
        TestAssert.Contains(help, "Available --trace-* options:", "bare trace root should print inline help");
        TestAssert.Contains(help, "--trace-examples", "trace help should list selector examples");
        TestAssert.Contains(help, "--trace-functions", "trace help should list function output control");

        string examples = TestConsole.CaptureStdout(() => parser.ParseOrThrow(new[] { "--trace-examples" }));
        TestAssert.Contains(examples, "Trace selector examples:", "trace examples command should print selector examples");
        TestAssert.Contains(examples, "--trace '.abc'", "trace examples should describe local selectors");
        TestAssert.Contains(examples, "--trace 'alpha.*.*.*'", "trace examples should include depth-three namespace patterns");
        TestAssert.Contains(examples, "--trace beta.{io,scheduler}.packet", "trace examples should include brace-expanded nested selectors");

        string namespaces = TestConsole.CaptureStdout(() => parser.ParseOrThrow(new[] { "--trace-namespaces" }));
        TestAssert.Contains(namespaces, "Available trace namespaces:", "trace namespaces command should print a header");
        TestAssert.Contains(namespaces, "tests", "trace namespaces command should list registered namespaces");

        string channels = TestConsole.CaptureStdout(() => parser.ParseOrThrow(new[] { "--trace-channels" }));
        TestAssert.Contains(channels, "Available trace channels:", "trace channels command should print a header");
        TestAssert.Contains(channels, "tests.app", "trace channels command should list registered channels");

        string colors = TestConsole.CaptureStdout(() => parser.ParseOrThrow(new[] { "--trace-colors" }));
        TestAssert.Contains(colors, "Available trace colors:", "trace colors command should print a header");
        TestAssert.Contains(colors, "HotPink", "trace colors command should include named colors");

        parser.ParseOrThrow(new[] { "--trace", ".app" });
        TestAssert.True(logger.ShouldTraceChannel(trace, ".app"), "trace root value handler should enable local selectors");

        parser.ParseOrThrow(new[] { "--trace-functions" });
        OutputOptions functionOptions = logger.GetOutputOptions();
        TestAssert.True(functionOptions.Filenames, "trace-functions should enable filenames");
        TestAssert.True(functionOptions.LineNumbers, "trace-functions should enable line numbers");
        TestAssert.True(functionOptions.FunctionNames, "trace-functions should enable function names");

        parser.ParseOrThrow(new[] { "--trace-timestamps" });
        OutputOptions timestampOptions = logger.GetOutputOptions();
        TestAssert.True(timestampOptions.Timestamps, "trace-timestamps should enable timestamps");
    }
}

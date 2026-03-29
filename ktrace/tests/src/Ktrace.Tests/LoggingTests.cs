using Ktools.CSharp.Tests;

namespace Ktrace.Tests;

internal static class LoggingTests
{
    public static void Run()
    {
        VerifyLogPrefixes();
        VerifyTraceOutput();
        VerifyIgnoredChannelWarnings();
    }

    private static void VerifyLogPrefixes()
    {
        string output = TestConsole.CaptureStdout(() =>
        {
            Logger logger = new Logger();
            TraceLogger trace = new TraceLogger("tests");
            logger.AddTraceLogger(trace);
            logger.SetOutputOptions(new OutputOptions
            {
                Filenames = true,
                LineNumbers = true,
            });
            trace.Info("info message");
            trace.Warn("warn value {}", 7);
            trace.Error("error message");
        });

        TestAssert.Contains(output, "[tests] [info]", "info prefix should include namespace and severity");
        TestAssert.Contains(output, "[tests] [warning]", "warning prefix should include namespace and severity");
        TestAssert.Contains(output, "[tests] [error]", "error prefix should include namespace and severity");
    }

    private static void VerifyTraceOutput()
    {
        string output = TestConsole.CaptureStdout(() =>
        {
            Logger logger = new Logger();
            TraceLogger trace = new TraceLogger("tests");
            trace.AddChannel("trace", "HotPink");
            logger.AddTraceLogger(trace);
            logger.EnableChannel("tests.trace");
            trace.Trace("trace", "member {} {{ok}}", 42);
        });

        TestAssert.Contains(output, "[tests] [trace]", "trace output should include namespace and channel");
        TestAssert.Contains(output, "member 42 {ok}", "trace output should contain formatted trace text");
    }

    private static void VerifyIgnoredChannelWarnings()
    {
        Logger logger = new Logger();
        TraceLogger trace = new TraceLogger("tests");
        trace.AddChannel("net");
        logger.AddTraceLogger(trace);
        logger.SetOutputOptions(new OutputOptions
        {
            Filenames = true,
            LineNumbers = true,
        });

        int exactLine = TestFixtures.NextSourceLine();
        string exactOutput = TestConsole.CaptureStdout(() => logger.EnableChannel(trace, ".missing"));
        TestAssert.Contains(exactOutput, "[tests] [warning]", "exact ignored-channel warning should include the local namespace");
        TestAssert.Contains(exactOutput, $"[LoggingTests:{exactLine}]", "exact ignored-channel warning should report the public call site");
        TestAssert.Contains(exactOutput, "enable ignored channel 'tests.missing' because it is not registered", "exact ignored-channel warning text should match C++");

        int selectorLine = TestFixtures.NextSourceLine();
        string selectorOutput = TestConsole.CaptureStdout(() => logger.EnableChannels(trace, ".missing"));
        TestAssert.Contains(selectorOutput, "[tests] [warning]", "ignored selector warning should include the local namespace");
        TestAssert.Contains(selectorOutput, $"[LoggingTests:{selectorLine}]", "ignored selector warning should report the public call site");
        TestAssert.Contains(selectorOutput, "enable ignored channel selector 'tests.missing' because it matched no registered channels", "ignored selector warning text should match C++");
    }
}

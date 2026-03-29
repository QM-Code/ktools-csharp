using System;
using Ktools.CSharp.Tests;

namespace Ktrace.Tests;

internal static class FormatTests
{
    public static void Run()
    {
        TestAssert.Equal(TraceFormatter.FormatMessage("value {} {}", 7, "done"), "value 7 done", "format placeholders should be substituted");
        TestAssert.Equal(TraceFormatter.FormatMessage("escaped {{}}"), "escaped {}", "escaped braces should be preserved");
        TestAssert.Equal(TraceFormatter.FormatMessage("bool {}", true), "bool true", "bools should format lower-case");

        TestAssert.Throws<ArgumentException>(() => TraceFormatter.FormatMessage("value {} {}", 7), "missing arguments should fail");
        TestAssert.Throws<ArgumentException>(() => TraceFormatter.FormatMessage("{"), "unterminated braces should fail");
        TestAssert.Throws<ArgumentException>(() => TraceFormatter.FormatMessage("{:x}", 7), "unsupported format specifiers should fail");
        TestAssert.True(Array.IndexOf(TraceFormatter.ColorNames, "HotPink") >= 0, "C# trace colors should include the C++ named color surface");
        TestAssert.True(Array.IndexOf(TraceFormatter.ColorNames, "Grey93") >= 0, "C# trace colors should include the extended grayscale colors");

        string output = TestConsole.CaptureStdout(() =>
        {
            Logger logger = new Logger();
            TraceLogger trace = new TraceLogger("tests");
            logger.AddTraceLogger(trace);
            trace.Warn("escaped {{}} {}", 7);
        });

        TestAssert.Contains(output, "escaped {} 7", "warn output should contain formatted text");
    }
}

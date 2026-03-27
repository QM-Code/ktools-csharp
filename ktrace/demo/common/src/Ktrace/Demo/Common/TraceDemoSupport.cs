using System;
using Ktrace;

namespace Ktrace.Demo.Common;

internal static class TraceDemoSupport
{
    public static void PrintSummary(string title)
    {
        Console.WriteLine();
        Console.WriteLine(title);
        Console.WriteLine();
    }

    public static void EmitSampleTrace(TraceLogger trace, string channel, string text)
    {
        trace.Trace(channel, text);
    }
}

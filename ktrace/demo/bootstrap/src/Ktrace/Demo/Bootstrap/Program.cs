using System;
using Ktrace;

namespace Ktrace.Demo.Bootstrap;

public static class Program
{
    public static void Main(string[] args)
    {
        Logger logger = new Logger();
        TraceLogger trace = new TraceLogger("bootstrap");
        trace.AddChannel("bootstrap", "BrightGreen");
        logger.AddTraceLogger(trace);
        logger.EnableChannel(trace, ".bootstrap");
        trace.Trace("bootstrap", "ktrace bootstrap compile/link check");
        Console.WriteLine();
        Console.WriteLine("Bootstrap succeeded.");
        Console.WriteLine();
    }
}

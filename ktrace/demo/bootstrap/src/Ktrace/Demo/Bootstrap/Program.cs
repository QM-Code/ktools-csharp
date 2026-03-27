using Ktrace;
using Ktrace.Demo.Common;

namespace Ktrace.Demo.Bootstrap;

public static class Program
{
    public static void Main(string[] args)
    {
        Logger logger = new Logger();
        TraceLogger trace = new TraceLogger("bootstrap");
        logger.AddTraceLogger(trace);
        trace.Info("ktrace csharp bootstrap import/log check passed");
        TraceDemoSupport.PrintSummary("KTRACE csharp bootstrap import/log check passed");
    }
}

using Ktrace;

namespace Ktrace.Demo.Beta;

public static class BetaTrace
{
    public static TraceLogger GetTraceLogger()
    {
        TraceLogger trace = new TraceLogger("beta");
        trace.AddChannel("io", "BrightGreen");
        trace.AddChannel("scheduler");
        trace.AddChannel("scheduler.tick");
        return trace;
    }
}

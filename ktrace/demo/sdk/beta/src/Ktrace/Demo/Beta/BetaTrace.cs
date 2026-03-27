using Ktrace;

namespace Ktrace.Demo.Beta;

public static class BetaTrace
{
    private static readonly TraceLogger Trace = CreateTraceLogger();

    public static TraceLogger GetTraceLogger()
    {
        return Trace;
    }

    public static void TestTraceLoggingChannels()
    {
        TraceLogger trace = GetTraceLogger();
        trace.Trace("io", "beta trace test on channel 'io'");
        trace.Trace("scheduler", "beta trace test on channel 'scheduler'");
    }

    private static TraceLogger CreateTraceLogger()
    {
        TraceLogger trace = new TraceLogger("beta");
        trace.AddChannel("io", "MediumSpringGreen");
        trace.AddChannel("scheduler", "Orange3");
        return trace;
    }
}

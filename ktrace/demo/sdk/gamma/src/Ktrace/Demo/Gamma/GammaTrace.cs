using Ktrace;

namespace Ktrace.Demo.Gamma;

public static class GammaTrace
{
    private static readonly TraceLogger Trace = CreateTraceLogger();

    public static TraceLogger GetTraceLogger()
    {
        return Trace;
    }

    public static void TestTraceLoggingChannels()
    {
        TraceLogger trace = GetTraceLogger();
        trace.Trace("physics", "gamma trace test on channel 'physics'");
        trace.Trace("metrics", "gamma trace test on channel 'metrics'");
    }

    private static TraceLogger CreateTraceLogger()
    {
        TraceLogger trace = new TraceLogger("gamma");
        trace.AddChannel("physics", "MediumOrchid1");
        trace.AddChannel("metrics", "LightSkyBlue1");
        return trace;
    }
}

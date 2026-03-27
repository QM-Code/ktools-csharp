using Ktrace;

namespace Ktrace.Demo.Alpha;

public static class AlphaTrace
{
    private static readonly TraceLogger Trace = CreateTraceLogger();

    public static TraceLogger GetTraceLogger()
    {
        return Trace;
    }

    public static void TestTraceLoggingChannels()
    {
        TraceLogger trace = GetTraceLogger();
        trace.Trace("net", "testing...");
        trace.Trace("net.alpha", "testing...");
        trace.Trace("net.beta", "testing...");
        trace.Trace("net.gamma", "testing...");
        trace.Trace("net.gamma.deep", "testing...");
        trace.Trace("cache", "testing...");
        trace.Trace("cache.gamma", "testing...");
        trace.Trace("cache.delta", "testing...");
        trace.Trace("cache.special", "testing...");
    }

    public static void TestStandardLoggingChannels()
    {
        TraceLogger trace = GetTraceLogger();
        trace.Info("testing...");
        trace.Warn("testing...");
        trace.Error("testing...");
    }

    private static TraceLogger CreateTraceLogger()
    {
        TraceLogger trace = new TraceLogger("alpha");
        trace.AddChannel("net", "DeepSkyBlue1");
        trace.AddChannel("net.alpha");
        trace.AddChannel("net.beta");
        trace.AddChannel("net.gamma");
        trace.AddChannel("net.gamma.deep");
        trace.AddChannel("cache", "Gold3");
        trace.AddChannel("cache.gamma", "Gold3");
        trace.AddChannel("cache.delta");
        trace.AddChannel("cache.special", "Red");
        return trace;
    }
}

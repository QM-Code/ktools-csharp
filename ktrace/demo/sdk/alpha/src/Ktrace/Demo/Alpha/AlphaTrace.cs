using Ktrace;

namespace Ktrace.Demo.Alpha;

public static class AlphaTrace
{
    public static TraceLogger GetTraceLogger()
    {
        TraceLogger trace = new TraceLogger("alpha");
        trace.AddChannel("net", "DeepSkyBlue1");
        trace.AddChannel("cache", "Gold3");
        return trace;
    }
}

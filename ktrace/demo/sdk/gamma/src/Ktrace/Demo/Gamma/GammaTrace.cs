using Ktrace;

namespace Ktrace.Demo.Gamma;

public static class GammaTrace
{
    public static TraceLogger GetTraceLogger()
    {
        TraceLogger trace = new TraceLogger("gamma");
        trace.AddChannel("physics");
        trace.AddChannel("physics.step", "BrightMagenta");
        return trace;
    }
}

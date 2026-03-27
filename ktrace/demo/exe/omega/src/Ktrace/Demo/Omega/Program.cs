using Kcli;
using Ktrace;
using Ktrace.Demo.Alpha;
using Ktrace.Demo.Beta;
using Ktrace.Demo.Common;
using Ktrace.Demo.Gamma;

namespace Ktrace.Demo.Omega;

public static class Program
{
    public static void Main(string[] args)
    {
        Logger logger = new Logger();
        TraceLogger trace = new TraceLogger("omega");
        trace.AddChannel("app", "BrightCyan");
        trace.AddChannel("orchestrator", "BrightYellow");
        trace.AddChannel("deep");
        trace.AddChannel("deep.branch");
        trace.AddChannel("deep.branch.leaf", "LightSalmon1");

        logger.AddTraceLogger(trace);
        logger.AddTraceLogger(AlphaTrace.GetTraceLogger());
        logger.AddTraceLogger(BetaTrace.GetTraceLogger());
        logger.AddTraceLogger(GammaTrace.GetTraceLogger());

        logger.EnableChannel(trace, ".app");
        trace.Trace("app", "omega initialized local trace channels");
        logger.DisableChannel(trace, ".app");

        Parser parser = new Parser();
        parser.AddInlineParser(logger.MakeInlineParser(trace));
        parser.ParseOrExit(args);

        trace.Trace("app", "cli processing enabled, use --trace for options");
        trace.Trace("app", "testing external tracing, use --trace '*.*' to view top-level channels");
        trace.Trace("deep.branch.leaf", "omega trace test on channel 'deep.branch.leaf'");
        AlphaTrace.TestTraceLoggingChannels();
        BetaTrace.TestTraceLoggingChannels();
        GammaTrace.TestTraceLoggingChannels();
        AlphaTrace.TestStandardLoggingChannels();
        trace.Trace("orchestrator", "omega completed imported SDK trace checks");
        trace.Info("testing...");
        trace.Warn("testing...");
        trace.Error("testing...");
        TraceDemoSupport.PrintSummary("KTRACE csharp demo omega import/integration check passed");
    }
}

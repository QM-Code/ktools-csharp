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
        TraceLogger appTrace = new TraceLogger("omega");
        appTrace.AddChannel("app", "BrightCyan");
        appTrace.AddChannel("lifecycle", "BrightYellow");

        logger.AddTraceLogger(appTrace);
        logger.AddTraceLogger(AlphaTrace.GetTraceLogger());
        logger.AddTraceLogger(BetaTrace.GetTraceLogger());
        logger.AddTraceLogger(GammaTrace.GetTraceLogger());

        Parser parser = new Parser();
        parser.AddInlineParser(logger.MakeInlineParser(appTrace));
        parser.ParseOrExit(args);

        logger.EnableChannels(appTrace, ".app,alpha.net,beta.scheduler.tick,gamma.physics.step");
        appTrace.Trace("app", "omega trace active");
        appTrace.Info("omega logger ready");
        TraceDemoSupport.PrintSummary("KTRACE csharp demo omega import/integration check passed");
    }
}

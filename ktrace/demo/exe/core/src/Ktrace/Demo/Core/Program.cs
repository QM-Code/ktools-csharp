using Kcli;
using Ktrace;
using Ktrace.Demo.Alpha;
using Ktrace.Demo.Common;

namespace Ktrace.Demo.Core;

public static class Program
{
    public static void Main(string[] args)
    {
        Logger logger = new Logger();
        TraceLogger appTrace = new TraceLogger("core");
        appTrace.AddChannel("app", "BrightCyan");
        appTrace.AddChannel("startup", "BrightYellow");

        logger.AddTraceLogger(appTrace);
        logger.AddTraceLogger(AlphaTrace.GetTraceLogger());

        Parser parser = new Parser();
        parser.AddInlineParser(logger.MakeInlineParser(appTrace));
        parser.ParseOrExit(args);

        logger.EnableChannel(appTrace, ".app");
        appTrace.Trace("app", "core trace active");
        appTrace.Info("core logger ready");
        TraceDemoSupport.PrintSummary("KTRACE csharp demo core import/integration check passed");
    }
}

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
        TraceLogger trace = new TraceLogger("core");
        trace.AddChannel("app", "BrightCyan");
        trace.AddChannel("startup", "BrightYellow");

        logger.AddTraceLogger(trace);
        logger.AddTraceLogger(AlphaTrace.GetTraceLogger());

        logger.EnableChannel(trace, ".app");
        trace.Trace("app", "core initialized local trace channels");

        Parser parser = new Parser();
        parser.AddInlineParser(logger.MakeInlineParser(trace));
        parser.ParseOrExit(args);

        trace.Trace("app", "cli processing enabled, use --trace for options");
        trace.Trace("startup", "testing imported tracing, use --trace '*.*' to view imported channels");
        AlphaTrace.TestTraceLoggingChannels();
    }
}

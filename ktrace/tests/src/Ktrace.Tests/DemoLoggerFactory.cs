using Kcli;

namespace Ktrace.Tests;

internal static class DemoLoggerFactory
{
    public static void CreateBootstrap(out Logger logger, out TraceLogger trace)
    {
        logger = new Logger();
        trace = new TraceLogger("bootstrap");
        trace.AddChannel("bootstrap", "BrightGreen");
        logger.AddTraceLogger(trace);
        logger.EnableChannel(trace, ".bootstrap");
    }

    public static Parser CreateCoreParser(out Logger logger, out TraceLogger trace)
    {
        logger = new Logger();
        trace = new TraceLogger("core");
        trace.AddChannel("app", "BrightCyan");
        trace.AddChannel("startup", "BrightYellow");

        logger.AddTraceLogger(trace);
        logger.AddTraceLogger(CreateAlphaTraceLogger());

        Parser parser = new Parser();
        parser.AddInlineParser(logger.MakeInlineParser(trace));
        return parser;
    }

    public static Parser CreateOmegaParser(out Logger logger, out TraceLogger trace)
    {
        logger = new Logger();
        trace = new TraceLogger("omega");
        trace.AddChannel("app", "BrightCyan");
        trace.AddChannel("orchestrator", "BrightYellow");
        trace.AddChannel("deep");
        trace.AddChannel("deep.branch");
        trace.AddChannel("deep.branch.leaf", "LightSalmon1");

        logger.AddTraceLogger(trace);
        logger.AddTraceLogger(CreateAlphaTraceLogger());
        logger.AddTraceLogger(CreateBetaTraceLogger());
        logger.AddTraceLogger(CreateGammaTraceLogger());

        Parser parser = new Parser();
        parser.AddInlineParser(logger.MakeInlineParser(trace));
        return parser;
    }

    private static TraceLogger CreateAlphaTraceLogger()
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

    private static TraceLogger CreateBetaTraceLogger()
    {
        TraceLogger trace = new TraceLogger("beta");
        trace.AddChannel("io", "MediumSpringGreen");
        trace.AddChannel("scheduler", "Orange3");
        return trace;
    }

    private static TraceLogger CreateGammaTraceLogger()
    {
        TraceLogger trace = new TraceLogger("gamma");
        trace.AddChannel("physics", "MediumOrchid1");
        trace.AddChannel("metrics", "LightSkyBlue1");
        return trace;
    }
}

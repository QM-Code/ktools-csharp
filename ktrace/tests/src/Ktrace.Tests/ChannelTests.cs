using System;
using Ktools.CSharp.Tests;

namespace Ktrace.Tests;

internal static class ChannelTests
{
    public static void Run()
    {
        VerifyExplicitOnOffSemantics();
        VerifyRegisteredChannelSemantics();
        VerifyTraceLoggerMergeSemantics();
        VerifyTraceLoggerAttachmentSemantics();
        VerifyParentColorInheritance();
    }

    private static void VerifyExplicitOnOffSemantics()
    {
        Logger logger = new Logger();
        TestFixtures.AddTestChannels(logger);

        logger.EnableChannels("tests.*");
        TestAssert.True(logger.ShouldTraceChannel("tests.net"), "tests.net should be enabled by tests.*");
        TestAssert.True(logger.ShouldTraceChannel("tests.cache"), "tests.cache should be enabled by tests.*");

        logger.DisableChannels("tests.*");
        TestAssert.True(!logger.ShouldTraceChannel("tests.net"), "tests.net should be disabled after tests.* disable");
        TestAssert.True(!logger.ShouldTraceChannel("tests.cache"), "tests.cache should be disabled after tests.* disable");

        logger.EnableChannel("tests.net");
        TestAssert.True(logger.ShouldTraceChannel("tests.net"), "explicit enable should turn tests.net back on");
        TestAssert.True(!logger.ShouldTraceChannel("tests.cache"), "tests.cache should stay off after explicit enable");

        logger.DisableChannel("tests.net");
        TestAssert.True(!logger.ShouldTraceChannel("tests.net"), "explicit disable should turn tests.net back off");
    }

    private static void VerifyRegisteredChannelSemantics()
    {
        Logger logger = new Logger();
        TestFixtures.AddTestChannels(logger);

        logger.EnableChannels("*.*.*");
        TestAssert.True(logger.ShouldTraceChannel("tests.store.requests"), "tests.store.requests should trace when explicitly registered and enabled");
        TestAssert.True(logger.ShouldTraceChannel("tests.net"), "tests.net should trace when *.*.* enables channels up to depth 2");
        TestAssert.True(!logger.ShouldTraceChannel("tests.bad name"), "invalid runtime channel names should not trace");

        logger.EnableChannel("tests.missing.child");
        TestAssert.True(!logger.ShouldTraceChannel("tests.missing.child"), "enableChannel should ignore unregistered exact channels");

        logger.EnableChannels("tests.missing.child");
        TestAssert.True(!logger.ShouldTraceChannel("tests.missing.child"), "enableChannels should ignore unresolved exact selectors");
    }

    private static void VerifyTraceLoggerMergeSemantics()
    {
        Logger logger = new Logger();

        TraceLogger first = new TraceLogger("tests");
        first.AddChannel("net");
        logger.AddTraceLogger(first);

        TraceLogger duplicate = new TraceLogger("tests");
        duplicate.AddChannel("net");
        logger.AddTraceLogger(duplicate);

        TraceLogger explicitColor = new TraceLogger("tests");
        explicitColor.AddChannel("net", "Gold3");
        logger.AddTraceLogger(explicitColor);

        TraceLogger conflictingColor = new TraceLogger("tests");
        conflictingColor.AddChannel("net", "Orange3");
        TestAssert.Throws<ArgumentException>(() => logger.AddTraceLogger(conflictingColor), "conflicting explicit channel colors should be rejected");
    }

    private static void VerifyTraceLoggerAttachmentSemantics()
    {
        TraceLogger trace = new TraceLogger("tests");
        trace.AddChannel("net");

        Logger first = new Logger();
        first.AddTraceLogger(trace);

        Logger second = new Logger();
        TestAssert.Throws<ArgumentException>(() => second.AddTraceLogger(trace), "trace logger should not attach to a second logger");
    }

    private static void VerifyParentColorInheritance()
    {
        Logger logger = new Logger();
        TraceLogger trace = new TraceLogger("tests");
        trace.AddChannel("net", "DeepSkyBlue1");
        trace.AddChannel("net.child");
        logger.AddTraceLogger(trace);

        string color = TestFixtures.ResolveChannelColor(logger, "tests", "net.child");
        TestAssert.Equal(color, "DeepSkyBlue1", "child channels should inherit their nearest registered parent color");
    }
}

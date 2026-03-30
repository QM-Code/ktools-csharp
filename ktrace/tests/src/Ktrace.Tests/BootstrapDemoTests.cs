using Ktools.CSharp.Tests;

namespace Ktrace.Tests;

internal static class BootstrapDemoTests
{
    public static void Run()
    {
        TestBootstrapDemoEmitsEnabledLocalTrace();
    }

    private static void TestBootstrapDemoEmitsEnabledLocalTrace()
    {
        DemoLoggerFactory.CreateBootstrap(out _, out TraceLogger trace);

        string output = TestConsole.CaptureStdout(() =>
        {
            trace.Trace("bootstrap", "ktrace bootstrap compile/link check");
        });

        TestAssert.Contains(output, "[bootstrap] [bootstrap]", "bootstrap demo should emit its enabled local channel");
        TestAssert.Contains(output, "ktrace bootstrap compile/link check", "bootstrap demo should emit the documented trace message");
    }
}

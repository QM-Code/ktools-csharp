namespace Ktrace.Tests;

internal static class KtraceTests
{
    public static void Run()
    {
        FormatTests.Run();
        ChannelTests.Run();
        LoggingTests.Run();
        CliTests.Run();
        ChangedTests.Run();
        BootstrapDemoTests.Run();
        CoreDemoTests.Run();
        OmegaDemoTests.Run();
    }
}

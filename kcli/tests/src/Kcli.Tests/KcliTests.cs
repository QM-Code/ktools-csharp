namespace Kcli.Tests;

internal static class KcliTests
{
    public static void Run()
    {
        ApiBehaviorTests.Run();
        AliasBehaviorTests.Run();
        InlineParserTests.Run();
        ValueHandlingTests.Run();
        BootstrapDemoTests.Run();
        CoreDemoTests.Run();
        OmegaDemoTests.Run();
        ErrorBehaviorTests.Run();
    }
}

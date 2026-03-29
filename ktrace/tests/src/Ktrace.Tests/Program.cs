using System;

namespace Ktrace.Tests;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            FormatTests.Run();
            ChannelTests.Run();
            LoggingTests.Run();
            CliTests.Run();
            ChangedTests.Run();
            Console.WriteLine("C# ktrace tests passed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }
}

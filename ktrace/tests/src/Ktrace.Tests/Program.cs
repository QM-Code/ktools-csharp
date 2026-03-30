using System;

namespace Ktrace.Tests;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            KtraceTests.Run();
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

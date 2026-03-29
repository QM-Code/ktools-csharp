using System;

namespace Kcli.Tests;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            KcliTests.Run();
            Console.WriteLine("C# kcli tests passed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }
}

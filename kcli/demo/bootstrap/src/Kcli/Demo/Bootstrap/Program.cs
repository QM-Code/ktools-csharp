using System;
using Kcli;

namespace Kcli.Demo.Bootstrap;

public static class Program
{
    public static void Main(string[] args)
    {
        Parser parser = new Parser();
        parser.AddAlias("-v", "--verbose");
        parser.SetHandler("--verbose", context => Console.WriteLine($"Processing {context.Option}"), "Enable verbose demo logging.");
        parser.ParseOrExit(args);
        Console.WriteLine();
        Console.WriteLine("KCLI csharp bootstrap import/parse check passed");
        Console.WriteLine();
    }
}

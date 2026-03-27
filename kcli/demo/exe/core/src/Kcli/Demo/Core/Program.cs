using System;
using Kcli;
using Kcli.Demo.Alpha;

namespace Kcli.Demo.Core;

public static class Program
{
    public static void Main(string[] args)
    {
        const string exeName = "kcli_demo_core";

        Parser parser = new Parser();
        parser.AddInlineParser(AlphaSdk.GetInlineParser());
        parser.AddAlias("-v", "--verbose");
        parser.AddAlias("-out", "--output");
        parser.AddAlias("-a", "--alpha-enable");
        parser.SetHandler("--verbose", _ => { }, "Enable verbose app logging.");
        parser.SetHandler("--output", (_, _) => { }, "Set app output target.");
        parser.ParseOrExit(args);

        Console.WriteLine();
        Console.WriteLine("KCLI csharp demo core import/integration check passed");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine($"  {exeName} --alpha");
        Console.WriteLine($"  {exeName} --output stdout");
        Console.WriteLine();
        Console.WriteLine("Enabled inline roots:");
        Console.WriteLine("  --alpha");
        Console.WriteLine();
    }
}

using System;
using Kcli;
using Kcli.Demo.Alpha;
using Kcli.Demo.Beta;
using Kcli.Demo.Gamma;

namespace Kcli.Demo.Omega;

public static class Program
{
    public static void Main(string[] args)
    {
        Parser parser = new Parser();
        InlineParser gammaParser = GammaSdk.GetInlineParser();
        InlineParser buildParser = new InlineParser("--build");

        gammaParser.SetRoot("--newgamma");
        buildParser.SetHandler("-profile", (_, _) => { }, "Set build profile.");
        buildParser.SetHandler("-clean", _ => { }, "Enable clean build.");

        parser.AddInlineParser(AlphaSdk.GetInlineParser());
        parser.AddInlineParser(BetaSdk.GetInlineParser());
        parser.AddInlineParser(gammaParser);
        parser.AddInlineParser(buildParser);

        parser.AddAlias("-v", "--verbose");
        parser.AddAlias("-out", "--output");
        parser.AddAlias("-a", "--alpha-enable");
        parser.AddAlias("-b", "--build-profile");

        parser.SetHandler("--verbose", _ => { }, "Enable verbose app logging.");
        parser.SetHandler("--output", (_, _) => { }, "Set app output target.");
        parser.SetPositionalHandler(_ => { });

        parser.ParseOrExit(args);

        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  kcli_demo_omega --<root>");
        Console.WriteLine();
        Console.WriteLine("Enabled --<root> prefixes:");
        Console.WriteLine("  --alpha");
        Console.WriteLine("  --beta");
        Console.WriteLine("  --newgamma (gamma override)");
        Console.WriteLine();
    }
}

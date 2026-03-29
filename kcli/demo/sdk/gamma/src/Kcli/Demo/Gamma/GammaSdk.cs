using System.Collections.Generic;
using Kcli;

namespace Kcli.Demo.Gamma;

public static class GammaSdk
{
    public static InlineParser GetInlineParser()
    {
        InlineParser parser = new InlineParser("--gamma");
        parser.SetOptionalValueHandler("-strict", PrintProcessingLine, "Enable strict gamma mode.");
        parser.SetHandler("-tag", PrintProcessingLine, "Set a gamma tag label.");
        return parser;
    }

    private static void PrintProcessingLine(HandlerContext context, string value)
    {
        if (context.ValueTokens.Count == 0)
        {
            System.Console.WriteLine($"Processing {context.Option}");
            return;
        }

        if (context.ValueTokens.Count == 1)
        {
            System.Console.WriteLine($"Processing {context.Option} with value \"{value}\"");
            return;
        }

        List<string> quoted = new List<string>();
        foreach (string token in context.ValueTokens)
        {
            quoted.Add($"\"{token}\"");
        }

        System.Console.WriteLine($"Processing {context.Option} with values [{string.Join(",", quoted)}]");
    }
}

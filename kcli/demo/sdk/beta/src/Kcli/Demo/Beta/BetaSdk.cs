using System;
using System.Collections.Generic;
using Kcli;

namespace Kcli.Demo.Beta;

public static class BetaSdk
{
    public static InlineParser GetInlineParser()
    {
        InlineParser parser = new InlineParser("--beta");
        parser.SetHandler("-profile", PrintProcessingLine, "Select beta runtime profile.");
        parser.SetHandler("-workers", (context, value) =>
        {
            if (!int.TryParse(value, out _))
            {
                throw new InvalidOperationException("expected an integer");
            }

            PrintProcessingLine(context, value);
        }, "Set beta worker count.");
        return parser;
    }

    private static void PrintProcessingLine(HandlerContext context, string value)
    {
        if (context.ValueTokens.Count == 0)
        {
            Console.WriteLine($"Processing {context.Option}");
            return;
        }

        if (context.ValueTokens.Count == 1)
        {
            Console.WriteLine($"Processing {context.Option} with value \"{value}\"");
            return;
        }

        List<string> quoted = new List<string>();
        foreach (string token in context.ValueTokens)
        {
            quoted.Add($"\"{token}\"");
        }

        Console.WriteLine($"Processing {context.Option} with values [{string.Join(",", quoted)}]");
    }
}

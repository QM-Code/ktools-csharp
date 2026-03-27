using System;
using System.Collections.Generic;
using Kcli;

namespace Kcli.Demo.Common;

public static class DemoSupport
{
    public static void PrintProcessingLine(HandlerContext context, string value)
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

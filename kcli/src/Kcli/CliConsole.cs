using System;
using System.Collections.Generic;

namespace Kcli;

internal static class CliConsole
{
    public static void ReportCliErrorAndExit(string message)
    {
        bool useColor = !Console.IsErrorRedirected;
        if (useColor)
        {
            Console.Error.WriteLine($"[\u001b[31merror\u001b[0m] [\u001b[94mcli\u001b[0m] {message}");
        }
        else
        {
            Console.Error.WriteLine($"[error] [cli] {message}");
        }

        Console.Error.Flush();
        Environment.Exit(2);
    }

    public static void PrintHelp(string root, IReadOnlyList<HelpRow> helpRows)
    {
        Console.WriteLine();
        Console.WriteLine($"Available --{root}-* options:");

        int maxLhs = 0;
        foreach (HelpRow row in helpRows)
        {
            if (row.Lhs.Length > maxLhs)
            {
                maxLhs = row.Lhs.Length;
            }
        }

        if (helpRows.Count == 0)
        {
            Console.WriteLine("  (no options registered)");
        }
        else
        {
            foreach (HelpRow row in helpRows)
            {
                Console.Write("  ");
                Console.Write(row.Lhs);
                Console.Write(new string(' ', (maxLhs - row.Lhs.Length) + 2));
                Console.WriteLine(row.Rhs);
            }
        }

        Console.WriteLine();
    }
}

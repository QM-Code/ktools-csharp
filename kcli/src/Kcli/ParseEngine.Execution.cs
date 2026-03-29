using System;
using System.Collections.Generic;

namespace Kcli;

internal static partial class ParseEngine
{
    private static void ExecuteInvocations(List<Invocation> invocations, MutableParseOutcome result)
    {
        foreach (Invocation invocation in invocations)
        {
            if (!result.Ok)
            {
                return;
            }

            if (invocation.Kind == InvocationKind.PrintHelp)
            {
                CliConsole.PrintHelp(invocation.Root, invocation.HelpRows);
                continue;
            }

            HandlerContext context = new HandlerContext(
                invocation.Root,
                invocation.Option,
                invocation.Command,
                invocation.ValueTokens);

            try
            {
                switch (invocation.Kind)
                {
                    case InvocationKind.Flag:
                        invocation.FlagHandler(context);
                        break;
                    case InvocationKind.Value:
                        invocation.ValueHandler(context, string.Join(" ", invocation.ValueTokens));
                        break;
                    case InvocationKind.Positional:
                        invocation.PositionalHandler(context);
                        break;
                }
            }
            catch (Exception ex)
            {
                result.ReportError(invocation.Option, FormatOptionErrorMessage(invocation.Option, ex.Message));
            }
        }
    }

    private static string FormatOptionErrorMessage(string option, string message)
    {
        if (string.IsNullOrEmpty(option))
        {
            return message ?? string.Empty;
        }

        return $"option '{option}': {message ?? string.Empty}";
    }
}

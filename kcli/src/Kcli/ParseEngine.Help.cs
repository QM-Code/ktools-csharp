using System.Collections.Generic;

namespace Kcli;

internal static partial class ParseEngine
{
    private static List<HelpRow> BuildHelpRows(InlineParserData parser)
    {
        List<HelpRow> rows = new List<HelpRow>();
        if (parser.RootValueHandler != null && parser.RootValueDescription.Length > 0)
        {
            string lhs = $"--{parser.RootName}";
            if (parser.RootValuePlaceholder.Length > 0)
            {
                lhs += $" {parser.RootValuePlaceholder}";
            }

            rows.Add(new HelpRow(lhs, parser.RootValueDescription));
        }

        foreach (string command in parser.CommandOrder)
        {
            CommandBinding binding = parser.Commands[command];
            string lhs = $"--{parser.RootName}-{command}";
            if (binding.ExpectsValue)
            {
                lhs += binding.ValueArity == ValueArity.Optional ? " [value]" : " <value>";
            }

            rows.Add(new HelpRow(lhs, binding.Description));
        }

        return rows;
    }

    private static InlineTokenMatch MatchInlineToken(ParserData data, string arg)
    {
        foreach (string rootName in data.InlineParserOrder)
        {
            InlineParserData parser = data.InlineParsers[rootName];
            string rootOption = $"--{parser.RootName}";
            if (arg == rootOption)
            {
                return new InlineTokenMatch
                {
                    Kind = InlineTokenKind.BareRoot,
                    Parser = parser,
                };
            }

            string rootDashPrefix = $"{rootOption}-";
            if (Normalization.StartsWith(arg, rootDashPrefix))
            {
                return new InlineTokenMatch
                {
                    Kind = InlineTokenKind.DashOption,
                    Parser = parser,
                    Suffix = arg.Substring(rootDashPrefix.Length),
                };
            }
        }

        return new InlineTokenMatch
        {
            Kind = InlineTokenKind.None,
        };
    }
}

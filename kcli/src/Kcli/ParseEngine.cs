using System;
using System.Collections.Generic;

namespace Kcli;

internal static class ParseEngine
{
    public static void Parse(ParserData data, int argc, string[] argv)
    {
        MutableParseOutcome result = new MutableParseOutcome();
        if (argc > 0 && argv == null)
        {
            result.ReportError(string.Empty, "kcli received invalid argv (argc > 0 but argv is null)");
            Normalization.ThrowCliError(result);
        }

        if (argc <= 0 || argv == null)
        {
            return;
        }

        if (argv.Length < argc)
        {
            result.ReportError(string.Empty, "kcli received invalid argv (argv shorter than argc)");
            Normalization.ThrowCliError(result);
        }

        bool[] consumed = new bool[argc];
        List<Invocation> invocations = new List<Invocation>();
        List<string> tokens = BuildParseTokens(argc, argv);

        int index = 1;
        while (index < argc)
        {
            if (consumed[index])
            {
                index += 1;
                continue;
            }

            string arg = tokens[index];
            if (arg.Length == 0)
            {
                index += 1;
                continue;
            }

            AliasBinding aliasBinding = null;
            string effectiveArg = arg;
            if (arg[0] == '-' && !Normalization.StartsWith(arg, "--"))
            {
                data.Aliases.TryGetValue(arg, out aliasBinding);
                if (aliasBinding != null)
                {
                    effectiveArg = aliasBinding.TargetToken;
                }
            }

            if (effectiveArg[0] != '-')
            {
                index += 1;
                continue;
            }

            if (effectiveArg == "--")
            {
                index += 1;
                continue;
            }

            if (Normalization.StartsWith(effectiveArg, "--"))
            {
                InlineTokenMatch inlineMatch = MatchInlineToken(data, effectiveArg);
                switch (inlineMatch.Kind)
                {
                    case InlineTokenKind.BareRoot:
                    {
                        ConsumeIndex(consumed, index);
                        CollectedValues collected = CollectValueTokens(index, tokens, consumed, false);
                        if (!collected.HasValue && !HasAliasPresetTokens(aliasBinding))
                        {
                            Invocation help = new Invocation
                            {
                                Kind = InvocationKind.PrintHelp,
                                Root = inlineMatch.Parser.RootName,
                            };
                            help.HelpRows.AddRange(BuildHelpRows(inlineMatch.Parser));
                            invocations.Add(help);
                        }
                        else if (inlineMatch.Parser.RootValueHandler == null)
                        {
                            result.ReportError(effectiveArg, $"unknown value for option '{effectiveArg}'");
                        }
                        else
                        {
                            Invocation invocation = new Invocation
                            {
                                Kind = InvocationKind.Value,
                                Root = inlineMatch.Parser.RootName,
                                Option = effectiveArg,
                                ValueHandler = inlineMatch.Parser.RootValueHandler,
                            };
                            invocation.ValueTokens.AddRange(BuildEffectiveValueTokens(aliasBinding, collected.Parts));
                            invocations.Add(invocation);
                            if (collected.HasValue)
                            {
                                index = collected.LastIndex;
                            }
                        }
                        break;
                    }
                    case InlineTokenKind.DashOption:
                    {
                        if (inlineMatch.Suffix.Length > 0 &&
                            inlineMatch.Parser.Commands.TryGetValue(inlineMatch.Suffix, out CommandBinding binding))
                        {
                            index = ScheduleInvocation(
                                binding,
                                aliasBinding,
                                inlineMatch.Parser.RootName,
                                inlineMatch.Suffix,
                                effectiveArg,
                                index,
                                tokens,
                                consumed,
                                invocations,
                                result);
                        }
                        break;
                    }
                    case InlineTokenKind.None:
                    {
                        string command = effectiveArg.Substring(2);
                        if (data.Commands.TryGetValue(command, out CommandBinding binding))
                        {
                            index = ScheduleInvocation(
                                binding,
                                aliasBinding,
                                string.Empty,
                                command,
                                effectiveArg,
                                index,
                                tokens,
                                consumed,
                                invocations,
                                result);
                        }
                        break;
                    }
                }
            }

            if (!result.Ok)
            {
                break;
            }

            index += 1;
        }

        if (result.Ok)
        {
            SchedulePositionals(data, tokens, consumed, invocations);
        }

        if (result.Ok)
        {
            for (int scan = 1; scan < argc; ++scan)
            {
                if (consumed[scan])
                {
                    continue;
                }

                string token = tokens[scan];
                if (token.Length > 0 && token[0] == '-')
                {
                    result.ReportError(token, $"unknown option {token}");
                    break;
                }
            }
        }

        if (result.Ok)
        {
            ExecuteInvocations(invocations, result);
        }

        if (!result.Ok)
        {
            Normalization.ThrowCliError(result);
        }
    }

    private static List<string> BuildParseTokens(int argc, string[] argv)
    {
        List<string> tokens = new List<string>(argc);
        for (int index = 0; index < argc; ++index)
        {
            tokens.Add(argv[index] ?? string.Empty);
        }
        return tokens;
    }

    private static bool IsCollectableFollowOnValueToken(string value)
    {
        return value.Length == 0 || value[0] != '-';
    }

    private static CollectedValues CollectValueTokens(int optionIndex, List<string> tokens, bool[] consumed, bool allowOptionLikeFirstValue)
    {
        CollectedValues collected = new CollectedValues
        {
            LastIndex = optionIndex,
        };

        int firstValueIndex = optionIndex + 1;
        bool hasNext = firstValueIndex >= 0 &&
            firstValueIndex < tokens.Count &&
            !consumed[firstValueIndex];
        if (!hasNext)
        {
            return collected;
        }

        string first = tokens[firstValueIndex];
        if (!allowOptionLikeFirstValue && first.Length > 0 && first[0] == '-')
        {
            return collected;
        }

        collected.HasValue = true;
        collected.Parts.Add(first);
        consumed[firstValueIndex] = true;
        collected.LastIndex = firstValueIndex;

        if (allowOptionLikeFirstValue && first.Length > 0 && first[0] == '-')
        {
            return collected;
        }

        for (int scan = firstValueIndex + 1; scan < tokens.Count; ++scan)
        {
            if (consumed[scan])
            {
                continue;
            }

            string next = tokens[scan];
            if (!IsCollectableFollowOnValueToken(next))
            {
                break;
            }

            collected.Parts.Add(next);
            consumed[scan] = true;
            collected.LastIndex = scan;
        }

        return collected;
    }

    private static void ConsumeIndex(bool[] consumed, int index)
    {
        if (index >= 0 && index < consumed.Length && !consumed[index])
        {
            consumed[index] = true;
        }
    }

    private static bool HasAliasPresetTokens(AliasBinding aliasBinding)
    {
        return aliasBinding != null && aliasBinding.PresetTokens.Count > 0;
    }

    private static List<string> BuildEffectiveValueTokens(AliasBinding aliasBinding, List<string> collectedParts)
    {
        List<string> merged = new List<string>();
        if (HasAliasPresetTokens(aliasBinding))
        {
            merged.AddRange(aliasBinding.PresetTokens);
        }
        merged.AddRange(collectedParts);
        return merged;
    }

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

    private static int ScheduleInvocation(
        CommandBinding binding,
        AliasBinding aliasBinding,
        string root,
        string command,
        string optionToken,
        int index,
        List<string> tokens,
        bool[] consumed,
        List<Invocation> invocations,
        MutableParseOutcome result)
    {
        ConsumeIndex(consumed, index);

        Invocation invocation = new Invocation
        {
            Root = root,
            Option = optionToken,
            Command = command,
        };

        if (!binding.ExpectsValue)
        {
            if (HasAliasPresetTokens(aliasBinding))
            {
                result.ReportError(
                    aliasBinding.Alias,
                    $"alias '{aliasBinding.Alias}' presets values for option '{optionToken}' which does not accept values");
                return index;
            }

            invocation.Kind = InvocationKind.Flag;
            invocation.FlagHandler = binding.FlagHandler;
            invocations.Add(invocation);
            return index;
        }

        CollectedValues collected = CollectValueTokens(index, tokens, consumed, binding.ValueArity == ValueArity.Required);
        if (!collected.HasValue && !HasAliasPresetTokens(aliasBinding) && binding.ValueArity == ValueArity.Required)
        {
            result.ReportError(optionToken, $"option '{optionToken}' requires a value");
            return index;
        }

        if (collected.HasValue)
        {
            index = collected.LastIndex;
        }

        invocation.Kind = InvocationKind.Value;
        invocation.ValueHandler = binding.ValueHandler;
        invocation.ValueTokens.AddRange(BuildEffectiveValueTokens(aliasBinding, collected.Parts));
        invocations.Add(invocation);
        return index;
    }

    private static void SchedulePositionals(ParserData data, List<string> tokens, bool[] consumed, List<Invocation> invocations)
    {
        if (data.PositionalHandler == null || tokens.Count <= 1)
        {
            return;
        }

        Invocation invocation = new Invocation
        {
            Kind = InvocationKind.Positional,
            PositionalHandler = data.PositionalHandler,
        };

        for (int index = 1; index < tokens.Count; ++index)
        {
            if (consumed[index])
            {
                continue;
            }

            string token = tokens[index];
            if (token.Length == 0 || token[0] != '-')
            {
                consumed[index] = true;
                invocation.ValueTokens.Add(token);
            }
        }

        if (invocation.ValueTokens.Count > 0)
        {
            invocations.Add(invocation);
        }
    }

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

            HandlerContext context = new HandlerContext(invocation.Root, invocation.Option, invocation.Command, invocation.ValueTokens);

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

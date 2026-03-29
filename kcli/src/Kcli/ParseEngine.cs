using System;
using System.Collections.Generic;

namespace Kcli;

internal static partial class ParseEngine
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
}

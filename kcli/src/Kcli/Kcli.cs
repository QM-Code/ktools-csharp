using System;
using System.Collections.Generic;
using System.IO;

namespace Kcli;

public sealed class CliError : Exception
{
    public CliError(string option, string message)
        : base(string.IsNullOrWhiteSpace(message) ? "kcli parse failed" : message)
    {
        Option = option ?? string.Empty;
    }

    public string Option { get; }
}

public sealed class HandlerContext
{
    public HandlerContext(string root, string option, string command, IReadOnlyList<string> valueTokens)
    {
        Root = root ?? string.Empty;
        Option = option ?? string.Empty;
        Command = command ?? string.Empty;
        ValueTokens = valueTokens ?? Array.Empty<string>();
    }

    public string Root { get; }
    public string Option { get; }
    public string Command { get; }
    public IReadOnlyList<string> ValueTokens { get; }
}

public delegate void FlagHandler(HandlerContext context);
public delegate void ValueHandler(HandlerContext context, string value);
public delegate void PositionalHandler(HandlerContext context);

public sealed class InlineParser
{
    private readonly InlineParserData _data;

    public InlineParser(string root)
    {
        _data = new InlineParserData();
        Registration.SetInlineRoot(_data, root);
    }

    public void SetRoot(string root)
    {
        Registration.SetInlineRoot(_data, root);
    }

    public void SetRootValueHandler(ValueHandler handler)
    {
        Registration.SetRootValueHandler(_data, handler);
    }

    public void SetRootValueHandler(ValueHandler handler, string valuePlaceholder, string description)
    {
        Registration.SetRootValueHandler(_data, handler, valuePlaceholder, description);
    }

    public void SetHandler(string option, FlagHandler handler, string description)
    {
        Registration.SetInlineHandler(_data, option, handler, description);
    }

    public void SetHandler(string option, ValueHandler handler, string description)
    {
        Registration.SetInlineHandler(_data, option, handler, description);
    }

    public void SetOptionalValueHandler(string option, ValueHandler handler, string description)
    {
        Registration.SetInlineOptionalValueHandler(_data, option, handler, description);
    }

    internal InlineParserData Snapshot()
    {
        return _data.Clone();
    }
}

public sealed class Parser
{
    private readonly ParserData _data = new ParserData();

    public void AddAlias(string alias, string target)
    {
        Registration.SetAlias(_data, alias, target, Array.Empty<string>());
    }

    public void AddAlias(string alias, string target, params string[] presetTokens)
    {
        Registration.SetAlias(_data, alias, target, presetTokens ?? Array.Empty<string>());
    }

    public void SetHandler(string option, FlagHandler handler, string description)
    {
        Registration.SetPrimaryHandler(_data, option, handler, description);
    }

    public void SetHandler(string option, ValueHandler handler, string description)
    {
        Registration.SetPrimaryHandler(_data, option, handler, description);
    }

    public void SetOptionalValueHandler(string option, ValueHandler handler, string description)
    {
        Registration.SetPrimaryOptionalValueHandler(_data, option, handler, description);
    }

    public void SetPositionalHandler(PositionalHandler handler)
    {
        Registration.SetPositionalHandler(_data, handler);
    }

    public void AddInlineParser(InlineParser parser)
    {
        if (parser == null)
        {
            throw new ArgumentNullException(nameof(parser), "kcli inline parser must not be empty");
        }

        Registration.AddInlineParser(_data, parser.Snapshot());
    }

    public void ParseOrExit(int argc, string[] argv)
    {
        try
        {
            ParseEngine.Parse(_data, argc, argv);
        }
        catch (CliError ex)
        {
            Normalization.ReportCliErrorAndExit(ex.Message);
        }
    }

    public void ParseOrThrow(int argc, string[] argv)
    {
        ParseEngine.Parse(_data, argc, argv);
    }

    public void ParseOrExit(string[] args)
    {
        string[] argv = WithProgramToken(args);
        ParseOrExit(argv.Length, argv);
    }

    public void ParseOrThrow(string[] args)
    {
        string[] argv = WithProgramToken(args);
        ParseOrThrow(argv.Length, argv);
    }

    private static string[] WithProgramToken(string[] args)
    {
        if (args == null)
        {
            return Array.Empty<string>();
        }

        string[] argv = new string[args.Length + 1];
        argv[0] = string.Empty;
        Array.Copy(args, 0, argv, 1, args.Length);
        return argv;
    }
}

internal enum ValueArity
{
    Required,
    Optional,
}

internal enum InvocationKind
{
    Flag,
    Value,
    Positional,
    PrintHelp,
}

internal enum InlineTokenKind
{
    None,
    BareRoot,
    DashOption,
}

internal sealed class CommandBinding
{
    public bool ExpectsValue { get; set; }
    public FlagHandler FlagHandler { get; set; }
    public ValueHandler ValueHandler { get; set; }
    public ValueArity ValueArity { get; set; } = ValueArity.Required;
    public string Description { get; set; } = string.Empty;

    public CommandBinding Clone()
    {
        return new CommandBinding
        {
            ExpectsValue = ExpectsValue,
            FlagHandler = FlagHandler,
            ValueHandler = ValueHandler,
            ValueArity = ValueArity,
            Description = Description,
        };
    }
}

internal sealed class AliasBinding
{
    public string Alias { get; set; } = string.Empty;
    public string TargetToken { get; set; } = string.Empty;
    public List<string> PresetTokens { get; } = new List<string>();

    public AliasBinding Clone()
    {
        AliasBinding copy = new AliasBinding
        {
            Alias = Alias,
            TargetToken = TargetToken,
        };
        copy.PresetTokens.AddRange(PresetTokens);
        return copy;
    }
}

internal sealed class ParserData
{
    public Dictionary<string, AliasBinding> Aliases { get; } = new Dictionary<string, AliasBinding>(StringComparer.Ordinal);
    public Dictionary<string, CommandBinding> Commands { get; } = new Dictionary<string, CommandBinding>(StringComparer.Ordinal);
    public List<string> CommandOrder { get; } = new List<string>();
    public Dictionary<string, InlineParserData> InlineParsers { get; } = new Dictionary<string, InlineParserData>(StringComparer.Ordinal);
    public List<string> InlineParserOrder { get; } = new List<string>();
    public PositionalHandler PositionalHandler { get; set; }
}

internal sealed class InlineParserData
{
    public string RootName { get; set; } = string.Empty;
    public ValueHandler RootValueHandler { get; set; }
    public string RootValuePlaceholder { get; set; } = string.Empty;
    public string RootValueDescription { get; set; } = string.Empty;
    public Dictionary<string, CommandBinding> Commands { get; } = new Dictionary<string, CommandBinding>(StringComparer.Ordinal);
    public List<string> CommandOrder { get; } = new List<string>();

    public InlineParserData Clone()
    {
        InlineParserData copy = new InlineParserData
        {
            RootName = RootName,
            RootValueHandler = RootValueHandler,
            RootValuePlaceholder = RootValuePlaceholder,
            RootValueDescription = RootValueDescription,
        };

        foreach (string command in CommandOrder)
        {
            copy.CommandOrder.Add(command);
            copy.Commands[command] = Commands[command].Clone();
        }

        return copy;
    }
}

internal sealed class MutableParseOutcome
{
    public bool Ok { get; private set; } = true;
    public string ErrorOption { get; private set; } = string.Empty;
    public string ErrorMessage { get; private set; } = string.Empty;

    public void ReportError(string option, string message)
    {
        if (!Ok)
        {
            return;
        }

        Ok = false;
        ErrorOption = option ?? string.Empty;
        ErrorMessage = message ?? string.Empty;
    }
}

internal sealed class Invocation
{
    public InvocationKind Kind { get; set; }
    public string Root { get; set; } = string.Empty;
    public string Option { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public List<string> ValueTokens { get; } = new List<string>();
    public FlagHandler FlagHandler { get; set; }
    public ValueHandler ValueHandler { get; set; }
    public PositionalHandler PositionalHandler { get; set; }
    public List<HelpRow> HelpRows { get; } = new List<HelpRow>();
}

internal sealed class CollectedValues
{
    public bool HasValue { get; set; }
    public List<string> Parts { get; } = new List<string>();
    public int LastIndex { get; set; } = -1;
}

internal sealed class HelpRow
{
    public HelpRow(string lhs, string rhs)
    {
        Lhs = lhs;
        Rhs = rhs;
    }

    public string Lhs { get; }
    public string Rhs { get; }
}

internal sealed class InlineTokenMatch
{
    public InlineTokenKind Kind { get; set; }
    public InlineParserData Parser { get; set; }
    public string Suffix { get; set; } = string.Empty;
}

internal static class Registration
{
    public static void SetInlineRoot(InlineParserData data, string root)
    {
        data.RootName = Normalization.NormalizeInlineRootOptionOrThrow(root);
    }

    public static void SetRootValueHandler(InlineParserData data, ValueHandler handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler), "kcli root value handler must not be empty");
        }

        data.RootValueHandler = handler;
        data.RootValuePlaceholder = string.Empty;
        data.RootValueDescription = string.Empty;
    }

    public static void SetRootValueHandler(InlineParserData data, ValueHandler handler, string valuePlaceholder, string description)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler), "kcli root value handler must not be empty");
        }

        data.RootValueHandler = handler;
        data.RootValuePlaceholder = Normalization.NormalizeHelpPlaceholderOrThrow(valuePlaceholder);
        data.RootValueDescription = Normalization.NormalizeDescriptionOrThrow(description);
    }

    public static void SetInlineHandler(InlineParserData data, string option, FlagHandler handler, string description)
    {
        string command = Normalization.NormalizeInlineHandlerOptionOrThrow(option, data.RootName);
        SetCommand(data.Commands, data.CommandOrder, command, MakeFlagBinding(handler, description));
    }

    public static void SetInlineHandler(InlineParserData data, string option, ValueHandler handler, string description)
    {
        string command = Normalization.NormalizeInlineHandlerOptionOrThrow(option, data.RootName);
        SetCommand(data.Commands, data.CommandOrder, command, MakeValueBinding(handler, description, ValueArity.Required));
    }

    public static void SetInlineOptionalValueHandler(InlineParserData data, string option, ValueHandler handler, string description)
    {
        string command = Normalization.NormalizeInlineHandlerOptionOrThrow(option, data.RootName);
        SetCommand(data.Commands, data.CommandOrder, command, MakeValueBinding(handler, description, ValueArity.Optional));
    }

    public static void SetAlias(ParserData data, string alias, string target, params string[] presetTokens)
    {
        AliasBinding binding = new AliasBinding
        {
            Alias = Normalization.NormalizeAliasOrThrow(alias),
            TargetToken = Normalization.NormalizeAliasTargetOptionOrThrow(target),
        };
        binding.PresetTokens.AddRange(presetTokens ?? Array.Empty<string>());
        data.Aliases[binding.Alias] = binding;
    }

    public static void SetPrimaryHandler(ParserData data, string option, FlagHandler handler, string description)
    {
        string command = Normalization.NormalizePrimaryHandlerOptionOrThrow(option);
        SetCommand(data.Commands, data.CommandOrder, command, MakeFlagBinding(handler, description));
    }

    public static void SetPrimaryHandler(ParserData data, string option, ValueHandler handler, string description)
    {
        string command = Normalization.NormalizePrimaryHandlerOptionOrThrow(option);
        SetCommand(data.Commands, data.CommandOrder, command, MakeValueBinding(handler, description, ValueArity.Required));
    }

    public static void SetPrimaryOptionalValueHandler(ParserData data, string option, ValueHandler handler, string description)
    {
        string command = Normalization.NormalizePrimaryHandlerOptionOrThrow(option);
        SetCommand(data.Commands, data.CommandOrder, command, MakeValueBinding(handler, description, ValueArity.Optional));
    }

    public static void SetPositionalHandler(ParserData data, PositionalHandler handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler), "kcli positional handler must not be empty");
        }

        data.PositionalHandler = handler;
    }

    public static void AddInlineParser(ParserData data, InlineParserData parser)
    {
        if (data.InlineParsers.ContainsKey(parser.RootName))
        {
            throw new ArgumentException($"kcli inline parser root '--{parser.RootName}' is already registered");
        }

        data.InlineParsers[parser.RootName] = parser.Clone();
        data.InlineParserOrder.Add(parser.RootName);
    }

    private static void SetCommand(
        Dictionary<string, CommandBinding> map,
        List<string> order,
        string command,
        CommandBinding binding)
    {
        if (!map.ContainsKey(command))
        {
            order.Add(command);
        }
        map[command] = binding;
    }

    private static CommandBinding MakeFlagBinding(FlagHandler handler, string description)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler), "kcli flag handler must not be empty");
        }

        return new CommandBinding
        {
            ExpectsValue = false,
            FlagHandler = handler,
            Description = Normalization.NormalizeDescriptionOrThrow(description),
        };
    }

    private static CommandBinding MakeValueBinding(ValueHandler handler, string description, ValueArity arity)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler), "kcli value handler must not be empty");
        }

        return new CommandBinding
        {
            ExpectsValue = true,
            ValueHandler = handler,
            ValueArity = arity,
            Description = Normalization.NormalizeDescriptionOrThrow(description),
        };
    }
}

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
                PrintHelp(invocation);
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
            catch
            {
                result.ReportError(invocation.Option, FormatOptionErrorMessage(invocation.Option, "unknown exception while handling option"));
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

    private static void PrintHelp(Invocation invocation)
    {
        Console.WriteLine();
        Console.WriteLine($"Available --{invocation.Root}-* options:");

        int maxLhs = 0;
        foreach (HelpRow row in invocation.HelpRows)
        {
            if (row.Lhs.Length > maxLhs)
            {
                maxLhs = row.Lhs.Length;
            }
        }

        if (invocation.HelpRows.Count == 0)
        {
            Console.WriteLine("  (no options registered)");
        }
        else
        {
            foreach (HelpRow row in invocation.HelpRows)
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

internal static class Normalization
{
    public static bool StartsWith(string value, string prefix)
    {
        return value.StartsWith(prefix, StringComparison.Ordinal);
    }

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

    public static string TrimWhitespace(string value)
    {
        return (value ?? string.Empty).Trim();
    }

    public static bool ContainsWhitespace(string value)
    {
        foreach (char character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                return true;
            }
        }
        return false;
    }

    public static string NormalizeRootNameOrThrow(string rawRoot)
    {
        string root = TrimWhitespace(rawRoot);
        if (root.Length == 0)
        {
            throw new ArgumentException("kcli root must not be empty");
        }
        if (root[0] == '-')
        {
            throw new ArgumentException("kcli root must not begin with '-'");
        }
        if (ContainsWhitespace(root))
        {
            throw new ArgumentException("kcli root is invalid");
        }
        return root;
    }

    public static string NormalizeInlineRootOptionOrThrow(string rawRoot)
    {
        string root = TrimWhitespace(rawRoot);
        if (root.Length == 0)
        {
            throw new ArgumentException("kcli root must not be empty");
        }
        if (StartsWith(root, "--"))
        {
            root = root.Substring(2);
        }
        else if (root[0] == '-')
        {
            throw new ArgumentException("kcli root must use '--root' or 'root'");
        }
        return NormalizeRootNameOrThrow(root);
    }

    public static string NormalizeInlineHandlerOptionOrThrow(string rawOption, string rootName)
    {
        string option = TrimWhitespace(rawOption);
        if (option.Length == 0)
        {
            throw new ArgumentException("kcli inline handler option must not be empty");
        }

        if (StartsWith(option, "--"))
        {
            string fullPrefix = $"--{rootName}-";
            if (!StartsWith(option, fullPrefix))
            {
                throw new ArgumentException($"kcli inline handler option must use '-name' or '{fullPrefix}name'");
            }
            option = option.Substring(fullPrefix.Length);
        }
        else if (option[0] == '-')
        {
            option = option.Substring(1);
        }
        else
        {
            throw new ArgumentException($"kcli inline handler option must use '-name' or '--{rootName}-name'");
        }

        return NormalizeCommandOrThrow(option);
    }

    public static string NormalizePrimaryHandlerOptionOrThrow(string rawOption)
    {
        string option = TrimWhitespace(rawOption);
        if (option.Length == 0)
        {
            throw new ArgumentException("kcli end-user handler option must not be empty");
        }

        if (StartsWith(option, "--"))
        {
            option = option.Substring(2);
        }
        else if (option[0] == '-')
        {
            throw new ArgumentException("kcli end-user handler option must use '--name' or 'name'");
        }

        return NormalizeCommandOrThrow(option);
    }

    public static string NormalizeAliasOrThrow(string rawAlias)
    {
        string alias = TrimWhitespace(rawAlias);
        if (alias.Length < 2 || alias[0] != '-' || StartsWith(alias, "--") || ContainsWhitespace(alias))
        {
            throw new ArgumentException("kcli alias must use single-dash form, e.g. '-v'");
        }
        return alias;
    }

    public static string NormalizeAliasTargetOptionOrThrow(string rawTarget)
    {
        string target = TrimWhitespace(rawTarget);
        if (target.Length < 3 || !StartsWith(target, "--") || ContainsWhitespace(target) || target[2] == '-')
        {
            throw new ArgumentException("kcli alias target must use double-dash form, e.g. '--verbose'");
        }
        return target;
    }

    public static string NormalizeHelpPlaceholderOrThrow(string rawPlaceholder)
    {
        string placeholder = TrimWhitespace(rawPlaceholder);
        if (placeholder.Length == 0)
        {
            throw new ArgumentException("kcli help placeholder must not be empty");
        }
        return placeholder;
    }

    public static string NormalizeDescriptionOrThrow(string rawDescription)
    {
        string description = TrimWhitespace(rawDescription);
        if (description.Length == 0)
        {
            throw new ArgumentException("kcli command description must not be empty");
        }
        return description;
    }

    public static void ThrowCliError(MutableParseOutcome result)
    {
        if (result.Ok)
        {
            throw new InvalidOperationException("kcli internal error: ThrowCliError called without a failure");
        }

        throw new CliError(result.ErrorOption, result.ErrorMessage);
    }

    private static string NormalizeCommandOrThrow(string command)
    {
        if (command.Length == 0)
        {
            throw new ArgumentException("kcli command must not be empty");
        }
        if (command[0] == '-')
        {
            throw new ArgumentException("kcli command must not start with '-'");
        }
        if (ContainsWhitespace(command))
        {
            throw new ArgumentException("kcli command must not contain whitespace");
        }
        return command;
    }
}

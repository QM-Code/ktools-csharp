using System;
using System.Collections.Generic;

namespace Kcli;

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

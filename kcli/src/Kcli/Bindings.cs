using System;
using System.Collections.Generic;

namespace Kcli;

internal enum ValueArity
{
    Required,
    Optional,
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

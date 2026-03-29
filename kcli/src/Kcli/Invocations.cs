using System.Collections.Generic;

namespace Kcli;

internal enum InvocationKind
{
    Flag,
    Value,
    Positional,
    PrintHelp,
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

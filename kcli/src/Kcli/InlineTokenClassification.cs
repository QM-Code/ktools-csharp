namespace Kcli;

internal enum InlineTokenKind
{
    None,
    BareRoot,
    DashOption,
}

internal sealed class InlineTokenMatch
{
    public InlineTokenKind Kind { get; set; }
    public InlineParserData Parser { get; set; }
    public string Suffix { get; set; } = string.Empty;
}

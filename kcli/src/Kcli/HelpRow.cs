namespace Kcli;

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

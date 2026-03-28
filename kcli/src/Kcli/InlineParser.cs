namespace Kcli;

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

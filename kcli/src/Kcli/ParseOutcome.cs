namespace Kcli;

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

using System;

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

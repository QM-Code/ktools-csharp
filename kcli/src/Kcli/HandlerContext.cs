using System;
using System.Collections.Generic;

namespace Kcli;

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

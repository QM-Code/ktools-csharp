using System;
using System.Collections.Generic;

namespace Ktrace;

internal sealed class TraceLoggerData
{
    public object SyncRoot { get; } = new object();
    public string TraceNamespace { get; set; } = string.Empty;
    public List<ChannelSpec> Channels { get; } = new List<ChannelSpec>();
    public Dictionary<string, string> ChangedKeys { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
    public Logger AttachedLogger { get; set; }
}

internal sealed class ChannelSpec
{
    public string Name { get; set; } = string.Empty;
    public string ColorName { get; set; } = string.Empty;
}

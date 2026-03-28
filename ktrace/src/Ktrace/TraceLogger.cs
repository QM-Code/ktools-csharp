using System;

namespace Ktrace;

public sealed class TraceLogger
{
    private readonly TraceLoggerData _data;

    public TraceLogger(string traceNamespace)
    {
        _data = new TraceLoggerData
        {
            TraceNamespace = TraceModel.NormalizeNamespaceOrThrow(traceNamespace),
        };
    }

    public string Namespace => _data.TraceNamespace;

    public void AddChannel(string channel, string colorName = "")
    {
        lock (_data.SyncRoot)
        {
            string normalized = TraceModel.NormalizeChannelOrThrow(channel);
            string normalizedColor = TraceModel.NormalizeColorName(colorName);
            TraceModel.AddChannelSpecOrThrow(_data, normalized, normalizedColor);
        }
    }

    public bool ShouldTraceChannel(string channel)
    {
        try
        {
            string normalized = TraceModel.NormalizeChannelOrThrow(channel);
            Logger attached = TraceModel.GetAttachedLogger(_data);
            return attached != null && attached.ShouldTrace(_data.TraceNamespace, normalized);
        }
        catch
        {
            return false;
        }
    }

    public void Trace(string channel, string formatText, params object[] args)
    {
        SourceContext source = SourceContext.Capture(1);
        TraceFormatted(channel, source, TraceFormatter.FormatMessage(formatText, args));
    }

    public void TraceChanged(string channel, object key, string formatText, params object[] args)
    {
        SourceContext source = SourceContext.Capture(1);
        if (!UpdateChangedKey(channel, source, TraceFormatter.FormatArgument(key)))
        {
            return;
        }

        TraceFormatted(channel, source, TraceFormatter.FormatMessage(formatText, args));
    }

    public void Info(string formatText, params object[] args)
    {
        LogFormatted(LogSeverity.Info, SourceContext.Capture(1), TraceFormatter.FormatMessage(formatText, args));
    }

    public void Warn(string formatText, params object[] args)
    {
        LogFormatted(LogSeverity.Warning, SourceContext.Capture(1), TraceFormatter.FormatMessage(formatText, args));
    }

    public void Error(string formatText, params object[] args)
    {
        LogFormatted(LogSeverity.Error, SourceContext.Capture(1), TraceFormatter.FormatMessage(formatText, args));
    }

    internal TraceLoggerData Data => _data;

    private void TraceFormatted(string channel, SourceContext source, string message)
    {
        string normalized = TraceModel.NormalizeChannelOrThrow(channel);
        Logger attached = TraceModel.GetAttachedLogger(_data);
        if (attached == null || !attached.ShouldTrace(_data.TraceNamespace, normalized))
        {
            return;
        }

        attached.EmitTrace(_data.TraceNamespace, normalized, source, message);
    }

    private bool UpdateChangedKey(string channel, SourceContext source, string key)
    {
        string normalized = TraceModel.NormalizeChannelOrThrow(channel);
        string siteKey = $"{source.FilePath}:{source.LineNumber}:{source.MemberName}:{normalized}";

        lock (_data.ChangedKeys)
        {
            if (_data.ChangedKeys.TryGetValue(siteKey, out string previous) && previous == key)
            {
                return false;
            }

            _data.ChangedKeys[siteKey] = key;
            return true;
        }
    }

    private void LogFormatted(LogSeverity severity, SourceContext source, string message)
    {
        Logger attached = TraceModel.GetAttachedLogger(_data);
        attached?.EmitLog(severity, _data.TraceNamespace, source, message);
    }
}

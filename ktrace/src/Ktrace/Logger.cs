using System;
using System.Collections.Generic;
using System.IO;

namespace Ktrace;

public sealed partial class Logger
{
    private readonly object _syncRoot = new object();
    private readonly HashSet<string> _enabledChannelKeys = new HashSet<string>(StringComparer.Ordinal);
    private readonly Dictionary<string, List<string>> _channelsByNamespace = new Dictionary<string, List<string>>(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, string>> _colorsByNamespace = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
    private readonly HashSet<string> _namespaces = new HashSet<string>(StringComparer.Ordinal);
    private readonly object _outputLock = new object();
    private OutputOptions _outputOptions;

    public void AddTraceLogger(TraceLogger logger)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        TraceLoggerData data = logger.Data;
        lock (_syncRoot)
        {
            TraceModel.EnsureTraceLoggerCanAttach(data, this);
            TraceModel.MergeTraceLoggerOrThrow(_channelsByNamespace, _colorsByNamespace, _namespaces, data);
            data.AttachedLogger = this;
        }
    }

    public void EnableChannel(string qualifiedChannel, string localNamespace = "")
    {
        ApplyExactChannelMutation(qualifiedChannel, localNamespace, enable: true);
    }

    public void EnableChannel(TraceLogger localTraceLogger, string qualifiedChannel)
    {
        EnableChannel(qualifiedChannel, ResolveLocalNamespace(localTraceLogger));
    }

    public void EnableChannels(string selectorsCsv, string localNamespace = "")
    {
        ApplySelectorMutation(selectorsCsv, localNamespace, enable: true);
    }

    public void EnableChannels(TraceLogger localTraceLogger, string selectorsCsv)
    {
        EnableChannels(selectorsCsv, ResolveLocalNamespace(localTraceLogger));
    }

    public bool ShouldTraceChannel(string qualifiedChannel, string localNamespace = "")
    {
        try
        {
            ExactChannelResolution resolution = TraceSelector.ResolveExactChannelOrThrow(this, qualifiedChannel, localNamespace);
            return resolution.Registered && ShouldTrace(resolution.TraceNamespace, resolution.Channel);
        }
        catch
        {
            return false;
        }
    }

    public bool ShouldTraceChannel(TraceLogger localTraceLogger, string qualifiedChannel)
    {
        return ShouldTraceChannel(qualifiedChannel, ResolveLocalNamespace(localTraceLogger));
    }

    public void DisableChannel(string qualifiedChannel, string localNamespace = "")
    {
        ApplyExactChannelMutation(qualifiedChannel, localNamespace, enable: false);
    }

    public void DisableChannel(TraceLogger localTraceLogger, string qualifiedChannel)
    {
        DisableChannel(qualifiedChannel, ResolveLocalNamespace(localTraceLogger));
    }

    public void DisableChannels(string selectorsCsv, string localNamespace = "")
    {
        ApplySelectorMutation(selectorsCsv, localNamespace, enable: false);
    }

    public void DisableChannels(TraceLogger localTraceLogger, string selectorsCsv)
    {
        DisableChannels(selectorsCsv, ResolveLocalNamespace(localTraceLogger));
    }

    public void SetOutputOptions(OutputOptions options)
    {
        if (!options.Filenames)
        {
            options.LineNumbers = false;
            options.FunctionNames = false;
        }

        _outputOptions = options;
    }

    public OutputOptions GetOutputOptions()
    {
        return _outputOptions;
    }

    public List<string> GetNamespaces()
    {
        lock (_syncRoot)
        {
            List<string> output = new List<string>(_namespaces);
            output.Sort(StringComparer.Ordinal);
            return output;
        }
    }

    public List<string> GetChannels(string traceNamespace)
    {
        string normalized = TraceModel.NormalizeNamespaceOrThrow(traceNamespace);
        lock (_syncRoot)
        {
            if (!_channelsByNamespace.TryGetValue(normalized, out List<string> channels))
            {
                return new List<string>();
            }

            List<string> output = new List<string>(channels);
            output.Sort(StringComparer.Ordinal);
            return output;
        }
    }

    internal bool ShouldTrace(string traceNamespace, string channel)
    {
        if (!TraceModel.IsValidChannelPath(channel))
        {
            return false;
        }

        lock (_syncRoot)
        {
            string key = TraceModel.MakeQualifiedChannelKey(traceNamespace, channel);
            return key.Length > 0 &&
                TraceModel.IsRegisteredTraceChannel(_channelsByNamespace, traceNamespace, channel) &&
                _enabledChannelKeys.Contains(key);
        }
    }

    internal void EmitTrace(string traceNamespace, string channel, SourceContext source, string message)
    {
        EmitLine(TraceFormatter.BuildTraceMessagePrefix(this, traceNamespace, channel, source), message);
    }

    internal void EmitLog(LogSeverity severity, string traceNamespace, SourceContext source, string message)
    {
        EmitLine(TraceFormatter.BuildLogMessagePrefix(this, traceNamespace, severity, source), message);
    }

    internal bool TryGetChannels(string traceNamespace, out List<string> channels)
    {
        lock (_syncRoot)
        {
            if (_channelsByNamespace.TryGetValue(traceNamespace, out List<string> existing))
            {
                channels = new List<string>(existing);
                return true;
            }
        }

        channels = new List<string>();
        return false;
    }

    internal bool TryGetColor(string traceNamespace, string channel, out string colorName)
    {
        lock (_syncRoot)
        {
            if (_colorsByNamespace.TryGetValue(traceNamespace, out Dictionary<string, string> colors))
            {
                string key = channel ?? string.Empty;
                while (key.Length > 0)
                {
                    if (colors.TryGetValue(key, out string existing))
                    {
                        colorName = existing;
                        return true;
                    }

                    int separator = key.LastIndexOf('.');
                    if (separator < 0)
                    {
                        break;
                    }

                    key = key.Substring(0, separator);
                }
            }
        }

        colorName = string.Empty;
        return false;
    }

    private static string ResolveLocalNamespace(TraceLogger localTraceLogger)
    {
        return localTraceLogger?.Namespace ?? string.Empty;
    }

    private void ApplyExactChannelMutation(string qualifiedChannel, string localNamespace, bool enable)
    {
        ExactChannelResolution resolution = TraceSelector.ResolveExactChannelOrThrow(this, qualifiedChannel, localNamespace);
        if (!resolution.Registered)
        {
            EmitIgnoredExactChannelWarning(localNamespace, resolution.Key, enable);
            return;
        }

        lock (_syncRoot)
        {
            SetChannelEnabledState(resolution.Key, enable);
        }
    }

    private void ApplySelectorMutation(string selectorsCsv, string localNamespace, bool enable)
    {
        SelectorResolution resolution = TraceSelector.ResolveSelectorExpressionOrThrow(this, selectorsCsv, localNamespace);
        lock (_syncRoot)
        {
            foreach (string key in resolution.ChannelKeys)
            {
                SetChannelEnabledState(key, enable);
            }
        }

        foreach (string selector in resolution.UnmatchedSelectors)
        {
            EmitIgnoredSelectorWarning(localNamespace, selector, enable);
        }
    }

    private void SetChannelEnabledState(string key, bool enable)
    {
        if (enable)
        {
            _enabledChannelKeys.Add(key);
        }
        else
        {
            _enabledChannelKeys.Remove(key);
        }
    }

    private void EmitIgnoredExactChannelWarning(string localNamespace, string key, bool enable)
    {
        string verb = enable ? "enable" : "disable";
        EmitLog(LogSeverity.Warning, localNamespace, SourceContext.Capture(1), $"{verb} ignored channel '{key}' because it is not registered");
    }

    private void EmitIgnoredSelectorWarning(string localNamespace, string selector, bool enable)
    {
        string verb = enable ? "enable" : "disable";
        EmitLog(LogSeverity.Warning, localNamespace, SourceContext.Capture(1), $"{verb} ignored channel selector '{selector}' because it matched no registered channels");
    }

    private void EmitLine(string prefix, string message)
    {
        lock (_outputLock)
        {
            Console.Write(prefix);
            Console.Write(" ");
            Console.WriteLine(message);
            Console.Out.Flush();
        }
    }
}

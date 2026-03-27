using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Kcli;

namespace Ktrace;

public struct OutputOptions
{
    public bool Filenames;
    public bool LineNumbers;
    public bool FunctionNames;
    public bool Timestamps;
}

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

public sealed class Logger
{
    private readonly object _syncRoot = new object();
    private readonly HashSet<string> _enabledChannelKeys = new HashSet<string>(StringComparer.Ordinal);
    private readonly Dictionary<string, List<string>> _channelsByNamespace = new Dictionary<string, List<string>>(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, string>> _colorsByNamespace = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
    private readonly HashSet<string> _namespaces = new HashSet<string>(StringComparer.Ordinal);
    private readonly List<TraceLoggerData> _attachedTraceLoggers = new List<TraceLoggerData>();
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
            if (!_attachedTraceLoggers.Contains(data))
            {
                _attachedTraceLoggers.Add(data);
            }
            data.AttachedLogger = this;
        }
    }

    public void EnableChannel(string qualifiedChannel, string localNamespace = "")
    {
        ExactChannelResolution resolution = TraceSelector.ResolveExactChannelOrThrow(this, qualifiedChannel, localNamespace);
        if (!resolution.Registered)
        {
            EmitLog(LogSeverity.Warning, localNamespace, SourceContext.Capture(1), $"enable ignored channel '{resolution.Key}' because it is not registered");
            return;
        }

        lock (_syncRoot)
        {
            _enabledChannelKeys.Add(resolution.Key);
        }
    }

    public void EnableChannel(TraceLogger localTraceLogger, string qualifiedChannel)
    {
        EnableChannel(qualifiedChannel, localTraceLogger?.Namespace ?? string.Empty);
    }

    public void EnableChannels(string selectorsCsv, string localNamespace = "")
    {
        SelectorResolution resolution = TraceSelector.ResolveSelectorExpressionOrThrow(this, selectorsCsv, localNamespace);
        lock (_syncRoot)
        {
            foreach (string key in resolution.ChannelKeys)
            {
                _enabledChannelKeys.Add(key);
            }
        }

        foreach (string selector in resolution.UnmatchedSelectors)
        {
            EmitLog(LogSeverity.Warning, localNamespace, SourceContext.Capture(1), $"enable ignored channel selector '{selector}' because it matched no registered channels");
        }
    }

    public void EnableChannels(TraceLogger localTraceLogger, string selectorsCsv)
    {
        EnableChannels(selectorsCsv, localTraceLogger?.Namespace ?? string.Empty);
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
        return ShouldTraceChannel(qualifiedChannel, localTraceLogger?.Namespace ?? string.Empty);
    }

    public void DisableChannel(string qualifiedChannel, string localNamespace = "")
    {
        ExactChannelResolution resolution = TraceSelector.ResolveExactChannelOrThrow(this, qualifiedChannel, localNamespace);
        if (!resolution.Registered)
        {
            EmitLog(LogSeverity.Warning, localNamespace, SourceContext.Capture(1), $"disable ignored channel '{resolution.Key}' because it is not registered");
            return;
        }

        lock (_syncRoot)
        {
            _enabledChannelKeys.Remove(resolution.Key);
        }
    }

    public void DisableChannel(TraceLogger localTraceLogger, string qualifiedChannel)
    {
        DisableChannel(qualifiedChannel, localTraceLogger?.Namespace ?? string.Empty);
    }

    public void DisableChannels(string selectorsCsv, string localNamespace = "")
    {
        SelectorResolution resolution = TraceSelector.ResolveSelectorExpressionOrThrow(this, selectorsCsv, localNamespace);
        lock (_syncRoot)
        {
            foreach (string key in resolution.ChannelKeys)
            {
                _enabledChannelKeys.Remove(key);
            }
        }

        foreach (string selector in resolution.UnmatchedSelectors)
        {
            EmitLog(LogSeverity.Warning, localNamespace, SourceContext.Capture(1), $"disable ignored channel selector '{selector}' because it matched no registered channels");
        }
    }

    public void DisableChannels(TraceLogger localTraceLogger, string selectorsCsv)
    {
        DisableChannels(selectorsCsv, localTraceLogger?.Namespace ?? string.Empty);
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

    public InlineParser MakeInlineParser(TraceLogger localTraceLogger, string traceRoot = "trace")
    {
        string localNamespace = localTraceLogger?.Namespace ?? string.Empty;
        InlineParser parser = new InlineParser(string.IsNullOrWhiteSpace(traceRoot) ? "trace" : traceRoot);
        parser.SetRootValueHandler((_, value) => EnableChannels(value, localNamespace), "<channels>", "Trace selected channels.");
        parser.SetHandler("-examples", context => PrintExamples(context.Root), "Show selector examples.");
        parser.SetHandler("-namespaces", _ => PrintNamespaces(), "Show initialized trace namespaces.");
        parser.SetHandler("-channels", _ => PrintChannels(), "Show initialized trace channels.");
        parser.SetHandler("-colors", _ => PrintColors(), "Show available trace colors.");
        parser.SetHandler("-files", _ =>
        {
            OutputOptions options = GetOutputOptions();
            options.Filenames = true;
            options.LineNumbers = true;
            SetOutputOptions(options);
        }, "Include source file and line in trace output.");
        parser.SetHandler("-functions", _ =>
        {
            OutputOptions options = GetOutputOptions();
            options.Filenames = true;
            options.LineNumbers = true;
            options.FunctionNames = true;
            SetOutputOptions(options);
        }, "Include function names in trace output.");
        parser.SetHandler("-timestamps", _ =>
        {
            OutputOptions options = GetOutputOptions();
            options.Timestamps = true;
            SetOutputOptions(options);
        }, "Include timestamps in trace output.");
        return parser;
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
            if (_colorsByNamespace.TryGetValue(traceNamespace, out Dictionary<string, string> colors) &&
                colors.TryGetValue(channel, out string existing))
            {
                colorName = existing;
                return true;
            }
        }

        colorName = string.Empty;
        return false;
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

    private void PrintNamespaces()
    {
        List<string> namespaces = GetNamespaces();
        if (namespaces.Count == 0)
        {
            Console.WriteLine("No trace namespaces defined.");
            Console.WriteLine();
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Available trace namespaces:");
        foreach (string traceNamespace in namespaces)
        {
            Console.WriteLine($"  {traceNamespace}");
        }
        Console.WriteLine();
    }

    private void PrintChannels()
    {
        bool printedAny = false;
        foreach (string traceNamespace in GetNamespaces())
        {
            foreach (string channel in GetChannels(traceNamespace))
            {
                if (!printedAny)
                {
                    Console.WriteLine();
                    Console.WriteLine("Available trace channels:");
                    printedAny = true;
                }
                Console.WriteLine($"  {traceNamespace}.{channel}");
            }
        }

        if (!printedAny)
        {
            Console.WriteLine("No trace channels defined.");
            Console.WriteLine();
            return;
        }
        Console.WriteLine();
    }

    private static void PrintColors()
    {
        Console.WriteLine();
        Console.WriteLine("Available trace colors:");
        foreach (string color in TraceFormatter.ColorNames)
        {
            Console.WriteLine($"  {color}");
        }
        Console.WriteLine();
    }

    private static void PrintExamples(string root)
    {
        string optionRoot = $"--{root}";
        Console.WriteLine();
        Console.WriteLine("General trace selector pattern:");
        Console.WriteLine($"  {optionRoot} <namespace>.<channel>[.<subchannel>[.<subchannel>]]");
        Console.WriteLine();
        Console.WriteLine("Trace selector examples:");
        Console.WriteLine($"  {optionRoot} '.abc'           Select local 'abc' in current namespace");
        Console.WriteLine($"  {optionRoot} '.abc.xyz'       Select local nested channel in current namespace");
        Console.WriteLine($"  {optionRoot} 'otherapp.channel' Select explicit namespace channel");
        Console.WriteLine($"  {optionRoot} '*.*'            Select all <namespace>.<channel> channels");
        Console.WriteLine($"  {optionRoot} '*.*.*'          Select all channels up to 2 levels");
        Console.WriteLine($"  {optionRoot} '*.*.*.*'        Select all channels up to 3 levels");
        Console.WriteLine($"  {optionRoot} '*.{{net,io}}'     Select 'net' and 'io' across all namespaces");
        Console.WriteLine($"  {optionRoot} '{{alpha,beta}}.*' Select all top-level channels in alpha and beta");
        Console.WriteLine();
    }
}

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

internal enum LogSeverity
{
    Info,
    Warning,
    Error,
}

internal readonly struct SourceContext
{
    public SourceContext(string filePath, int lineNumber, string memberName)
    {
        FilePath = filePath ?? string.Empty;
        LineNumber = lineNumber;
        MemberName = memberName ?? string.Empty;
    }

    public string FilePath { get; }
    public int LineNumber { get; }
    public string MemberName { get; }

    public static SourceContext Capture(int skipFrames)
    {
        StackTrace trace = new StackTrace(skipFrames + 1, true);
        StackFrame frame = trace.GetFrame(0);
        if (frame == null)
        {
            return new SourceContext(string.Empty, 0, string.Empty);
        }

        string memberName = frame.GetMethod()?.Name ?? string.Empty;
        return new SourceContext(frame.GetFileName() ?? string.Empty, frame.GetFileLineNumber(), memberName);
    }
}

internal sealed class Selector
{
    public bool AnyNamespace { get; set; }
    public string TraceNamespace { get; set; } = string.Empty;
    public string[] ChannelTokens { get; } = { string.Empty, string.Empty, string.Empty };
    public int ChannelDepth { get; set; }
    public bool IncludeTopLevel { get; set; }

    public string Format()
    {
        List<string> parts = new List<string>();
        for (int index = 0; index < ChannelDepth; ++index)
        {
            parts.Add(ChannelTokens[index]);
        }
        return $"{(AnyNamespace ? "*" : TraceNamespace)}.{string.Join(".", parts)}";
    }
}

internal sealed class SelectorResolution
{
    public List<string> ChannelKeys { get; } = new List<string>();
    public List<string> UnmatchedSelectors { get; } = new List<string>();
}

internal readonly struct ExactChannelResolution
{
    public ExactChannelResolution(string key, string traceNamespace, string channel, bool registered)
    {
        Key = key;
        TraceNamespace = traceNamespace;
        Channel = channel;
        Registered = registered;
    }

    public string Key { get; }
    public string TraceNamespace { get; }
    public string Channel { get; }
    public bool Registered { get; }
}

internal static class TraceModel
{
    public static string NormalizeNamespaceOrThrow(string traceNamespace)
    {
        string normalized = TrimWhitespace(traceNamespace);
        if (!IsSelectorIdentifier(normalized))
        {
            throw new ArgumentException($"invalid trace namespace '{normalized}'");
        }
        return normalized;
    }

    public static string NormalizeChannelOrThrow(string channel)
    {
        string normalized = TrimWhitespace(channel);
        if (!IsValidChannelPath(normalized))
        {
            throw new ArgumentException($"invalid trace channel '{normalized}'");
        }
        return normalized;
    }

    public static string NormalizeColorName(string colorName)
    {
        string normalized = TrimWhitespace(colorName);
        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        foreach (string color in TraceFormatter.ColorNames)
        {
            if (string.Equals(color, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return color;
            }
        }

        throw new ArgumentException($"unknown trace color '{normalized}'");
    }

    public static void AddChannelSpecOrThrow(TraceLoggerData data, string channel, string colorName)
    {
        int separator = channel.LastIndexOf('.');
        if (separator >= 0)
        {
            string parent = channel.Substring(0, separator);
            bool hasParent = false;
            foreach (ChannelSpec existing in data.Channels)
            {
                if (existing.Name == parent)
                {
                    hasParent = true;
                    break;
                }
            }
            if (!hasParent)
            {
                throw new ArgumentException($"cannot add unparented trace channel '{channel}' (missing parent '{parent}')");
            }
        }

        foreach (ChannelSpec existing in data.Channels)
        {
            if (existing.Name != channel)
            {
                continue;
            }

            if (existing.ColorName.Length == 0)
            {
                existing.ColorName = colorName;
                return;
            }
            if (colorName.Length == 0 || existing.ColorName == colorName)
            {
                return;
            }
            throw new ArgumentException($"conflicting trace color for '{data.TraceNamespace}.{channel}'");
        }

        data.Channels.Add(new ChannelSpec { Name = channel, ColorName = colorName });
    }

    public static void EnsureTraceLoggerCanAttach(TraceLoggerData data, Logger logger)
    {
        if (data.AttachedLogger != null && !ReferenceEquals(data.AttachedLogger, logger))
        {
            throw new ArgumentException("trace logger is already attached to another logger");
        }
    }

    public static Logger GetAttachedLogger(TraceLoggerData data)
    {
        return data.AttachedLogger;
    }

    public static void MergeTraceLoggerOrThrow(
        Dictionary<string, List<string>> channelsByNamespace,
        Dictionary<string, Dictionary<string, string>> colorsByNamespace,
        HashSet<string> namespaces,
        TraceLoggerData traceLogger)
    {
        string traceNamespace = traceLogger.TraceNamespace;
        namespaces.Add(traceNamespace);

        if (!channelsByNamespace.TryGetValue(traceNamespace, out List<string> registeredChannels))
        {
            registeredChannels = new List<string>();
            channelsByNamespace[traceNamespace] = registeredChannels;
        }
        if (!colorsByNamespace.TryGetValue(traceNamespace, out Dictionary<string, string> registeredColors))
        {
            registeredColors = new Dictionary<string, string>(StringComparer.Ordinal);
            colorsByNamespace[traceNamespace] = registeredColors;
        }

        foreach (ChannelSpec channel in traceLogger.Channels)
        {
            int separator = channel.Name.LastIndexOf('.');
            if (separator >= 0)
            {
                string parent = channel.Name.Substring(0, separator);
                if (!registeredChannels.Contains(parent))
                {
                    throw new ArgumentException($"cannot register unparented trace channel '{channel.Name}' (missing parent '{parent}')");
                }
            }

            if (!registeredChannels.Contains(channel.Name))
            {
                registeredChannels.Add(channel.Name);
            }

            if (!registeredColors.TryGetValue(channel.Name, out string existingColor))
            {
                existingColor = string.Empty;
            }
            if (existingColor.Length == 0)
            {
                if (channel.ColorName.Length > 0)
                {
                    registeredColors[channel.Name] = channel.ColorName;
                }
                continue;
            }
            if (channel.ColorName.Length > 0 && existingColor != channel.ColorName)
            {
                throw new ArgumentException($"conflicting trace color for '{traceNamespace}.{channel.Name}'");
            }
        }
    }

    public static string TrimWhitespace(string value)
    {
        return (value ?? string.Empty).Trim();
    }

    public static bool IsSelectorIdentifier(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }
        foreach (char character in token)
        {
            if (!(char.IsLetterOrDigit(character) || character == '_' || character == '-'))
            {
                return false;
            }
        }
        return true;
    }

    public static bool IsValidChannelPath(string channel)
    {
        if (string.IsNullOrEmpty(channel))
        {
            return false;
        }

        string[] parts = channel.Split('.');
        if (parts.Length == 0 || parts.Length > 3)
        {
            return false;
        }

        foreach (string part in parts)
        {
            if (!IsSelectorIdentifier(part))
            {
                return false;
            }
        }
        return true;
    }

    public static bool IsRegisteredTraceChannel(Dictionary<string, List<string>> channelsByNamespace, string traceNamespace, string channel)
    {
        return channelsByNamespace.TryGetValue(traceNamespace, out List<string> channels) && channels.Contains(channel);
    }

    public static string MakeQualifiedChannelKey(string traceNamespace, string channel)
    {
        string normalizedNamespace = TrimWhitespace(traceNamespace);
        string normalizedChannel = TrimWhitespace(channel);
        if (normalizedNamespace.Length == 0 || normalizedChannel.Length == 0)
        {
            return string.Empty;
        }
        return $"{normalizedNamespace}.{normalizedChannel}";
    }
}

internal static class TraceSelector
{
    public static ExactChannelResolution ResolveExactChannelOrThrow(Logger logger, string qualifiedChannel, string localNamespace)
    {
        string qualified = TraceModel.TrimWhitespace(qualifiedChannel);
        int separator = qualified.IndexOf('.');
        if (separator < 0)
        {
            throw new ArgumentException(
                $"invalid channel selector '{qualified}' (expected namespace.channel or .channel; use .channel for local namespace)");
        }

        string traceNamespace = separator == 0 ? TraceModel.TrimWhitespace(localNamespace) : qualified.Substring(0, separator);
        string channel = qualified.Substring(separator + 1);
        traceNamespace = TraceModel.NormalizeNamespaceOrThrow(traceNamespace);
        channel = TraceModel.NormalizeChannelOrThrow(channel);
        string key = TraceModel.MakeQualifiedChannelKey(traceNamespace, channel);
        bool registered = logger.TryGetChannels(traceNamespace, out List<string> channels) && channels.Contains(channel);
        return new ExactChannelResolution(key, traceNamespace, channel, registered);
    }

    public static SelectorResolution ResolveSelectorExpressionOrThrow(Logger logger, string selectorsCsv, string localNamespace)
    {
        string selectorText = TraceModel.TrimWhitespace(selectorsCsv);
        if (selectorText.Length == 0)
        {
            throw new ArgumentException("EnableChannels requires one or more selectors");
        }

        List<string> invalidTokens = new List<string>();
        List<Selector> selectors = ParseSelectorList(selectorText, localNamespace, invalidTokens);
        if (invalidTokens.Count > 0)
        {
            throw new InvalidOperationException(
                $"Invalid trace selector{(invalidTokens.Count > 1 ? "s" : string.Empty)}: {string.Join(", ", invalidTokens)}");
        }

        return ResolveSelectorsToChannelKeys(logger, selectors);
    }

    private static List<Selector> ParseSelectorList(string list, string localNamespace, List<string> invalidTokens)
    {
        List<Selector> selectors = new List<Selector>();
        HashSet<string> invalidSeen = new HashSet<string>(StringComparer.Ordinal);

        if (!SplitByTopLevelCommas(list, out List<string> selectorTokens, out string splitError))
        {
            invalidTokens.Add($"'{splitError}'");
            return selectors;
        }

        foreach (string token in selectorTokens)
        {
            string name = TraceModel.TrimWhitespace(token);
            if (name.Length == 0)
            {
                if (invalidSeen.Add("'<empty>'"))
                {
                    invalidTokens.Add("'<empty>'");
                }
                continue;
            }

            if (!ExpandBraceExpression(name, out List<string> expandedTokens, out string expandError))
            {
                string reason = $"'{name}' ({expandError})";
                if (invalidSeen.Add(reason))
                {
                    invalidTokens.Add(reason);
                }
                continue;
            }

            foreach (string expanded in expandedTokens)
            {
                if (!ParseSelectorExpression(expanded, localNamespace, out Selector selector, out string parseError))
                {
                    string reason = $"'{expanded}' ({parseError})";
                    if (invalidSeen.Add(reason))
                    {
                        invalidTokens.Add(reason);
                    }
                    continue;
                }

                selectors.Add(selector);
            }
        }

        return selectors;
    }

    private static SelectorResolution ResolveSelectorsToChannelKeys(Logger logger, List<Selector> selectors)
    {
        SelectorResolution result = new SelectorResolution();
        if (selectors.Count == 0)
        {
            return result;
        }

        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        bool[] matched = new bool[selectors.Count];

        foreach (string traceNamespace in logger.GetNamespaces())
        {
            foreach (string channel in logger.GetChannels(traceNamespace))
            {
                for (int index = 0; index < selectors.Count; ++index)
                {
                    if (!MatchesSelector(selectors[index], traceNamespace, channel))
                    {
                        continue;
                    }

                    matched[index] = true;
                    string key = TraceModel.MakeQualifiedChannelKey(traceNamespace, channel);
                    if (key.Length > 0 && seen.Add(key))
                    {
                        result.ChannelKeys.Add(key);
                    }
                }
            }
        }

        HashSet<string> unmatchedSeen = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < selectors.Count; ++index)
        {
            if (!matched[index])
            {
                string selectorText = selectors[index].Format();
                if (unmatchedSeen.Add(selectorText))
                {
                    result.UnmatchedSelectors.Add(selectorText);
                }
            }
        }

        return result;
    }

    private static bool MatchesSelector(Selector selector, string traceNamespace, string channel)
    {
        if (!selector.AnyNamespace && selector.TraceNamespace != traceNamespace)
        {
            return false;
        }

        string[] parts = channel.Split('.');
        int depth = parts.Length;

        if (selector.ChannelDepth == 1)
        {
            return depth == 1 && MatchesSelectorSegment(selector.ChannelTokens[0], parts[0]);
        }

        if (selector.ChannelDepth == 2)
        {
            if (depth == 1 && selector.IncludeTopLevel)
            {
                return true;
            }
            return depth == 2 &&
                MatchesSelectorSegment(selector.ChannelTokens[0], parts[0]) &&
                MatchesSelectorSegment(selector.ChannelTokens[1], parts[1]);
        }

        bool includeUpToDepthThree =
            selector.ChannelTokens[0] == "*" &&
            selector.ChannelTokens[1] == "*" &&
            selector.ChannelTokens[2] == "*";
        if (includeUpToDepthThree)
        {
            return depth >= 1 && depth <= 3;
        }

        return depth == 3 &&
            MatchesSelectorSegment(selector.ChannelTokens[0], parts[0]) &&
            MatchesSelectorSegment(selector.ChannelTokens[1], parts[1]) &&
            MatchesSelectorSegment(selector.ChannelTokens[2], parts[2]);
    }

    private static bool MatchesSelectorSegment(string pattern, string value)
    {
        return pattern == "*" || pattern == value;
    }

    private static bool SplitByTopLevelCommas(string value, out List<string> parts, out string error)
    {
        parts = new List<string>();
        error = string.Empty;
        int start = 0;
        int braceDepth = 0;

        for (int index = 0; index < value.Length; ++index)
        {
            char character = value[index];
            if (character == '{')
            {
                braceDepth += 1;
                continue;
            }
            if (character == '}')
            {
                if (braceDepth == 0)
                {
                    error = "unmatched '}'";
                    return false;
                }
                braceDepth -= 1;
                continue;
            }
            if (character == ',' && braceDepth == 0)
            {
                parts.Add(TraceModel.TrimWhitespace(value.Substring(start, index - start)));
                start = index + 1;
            }
        }

        if (braceDepth != 0)
        {
            error = "unmatched '{'";
            return false;
        }

        parts.Add(TraceModel.TrimWhitespace(value.Substring(start)));
        return true;
    }

    private static bool ExpandBraceExpression(string value, out List<string> expanded, out string error)
    {
        expanded = new List<string>();
        error = string.Empty;

        int open = value.IndexOf('{');
        if (open < 0)
        {
            expanded.Add(value);
            return true;
        }

        int depth = 0;
        int close = -1;
        for (int index = open; index < value.Length; ++index)
        {
            if (value[index] == '{')
            {
                depth += 1;
            }
            else if (value[index] == '}')
            {
                depth -= 1;
                if (depth == 0)
                {
                    close = index;
                    break;
                }
            }
        }

        if (close < 0)
        {
            error = "unmatched '{'";
            return false;
        }

        string prefix = value.Substring(0, open);
        string suffix = value.Substring(close + 1);
        string inside = value.Substring(open + 1, close - open - 1);
        if (!SplitByTopLevelCommas(inside, out List<string> alternatives, out error))
        {
            return false;
        }

        foreach (string alternative in alternatives)
        {
            if (alternative.Length == 0)
            {
                error = "empty brace alternative";
                return false;
            }
            if (!ExpandBraceExpression(prefix + alternative + suffix, out List<string> nested, out error))
            {
                return false;
            }
            expanded.AddRange(nested);
        }

        return true;
    }

    private static bool ParseSelectorExpression(string rawToken, string localNamespace, out Selector selector, out string error)
    {
        selector = new Selector();
        error = string.Empty;

        int separator = rawToken.IndexOf('.');
        if (separator < 0)
        {
            error = "did you mean '.*'?";
            return false;
        }

        string ns = rawToken.Substring(0, separator);
        string channelPattern = rawToken.Substring(separator + 1);
        if (ns == "*")
        {
            selector.AnyNamespace = true;
        }
        else if (ns.Length == 0)
        {
            string namespaceName = TraceModel.TrimWhitespace(localNamespace);
            if (!TraceModel.IsSelectorIdentifier(namespaceName))
            {
                error = "missing namespace";
                return false;
            }
            selector.TraceNamespace = namespaceName;
        }
        else if (TraceModel.IsSelectorIdentifier(ns))
        {
            selector.TraceNamespace = ns;
        }
        else
        {
            error = $"invalid namespace '{ns}'";
            return false;
        }

        if (!ParseSelectorChannelPattern(channelPattern, selector, out error))
        {
            return false;
        }

        return true;
    }

    private static bool ParseSelectorChannelPattern(string expression, Selector selector, out string error)
    {
        error = string.Empty;
        if (expression.Length == 0)
        {
            error = "missing channel expression";
            return false;
        }

        string[] parts = expression.Split('.');
        if (parts.Length == 0 || parts.Length > 3)
        {
            error = "channel depth exceeds 3";
            return false;
        }

        for (int index = 0; index < parts.Length; ++index)
        {
            string token = parts[index];
            if (token.Length == 0)
            {
                error = "empty channel token";
                return false;
            }
            if (token != "*" && !TraceModel.IsSelectorIdentifier(token))
            {
                error = $"invalid channel token '{token}'";
                return false;
            }
            selector.ChannelTokens[index] = token;
        }

        selector.ChannelDepth = parts.Length;
        selector.IncludeTopLevel = parts.Length == 2 &&
            selector.ChannelTokens[0] == "*" &&
            selector.ChannelTokens[1] == "*";
        return true;
    }
}

public static class TraceFormatter
{
    public static readonly string[] ColorNames =
    {
        "Default",
        "BrightCyan",
        "BrightYellow",
        "Gold3",
        "DeepSkyBlue1",
        "Orange3",
        "BrightGreen",
        "BrightMagenta",
    };

    public static string FormatArgument(object value)
    {
        if (value == null)
        {
            return string.Empty;
        }
        if (value is bool boolValue)
        {
            return boolValue ? "true" : "false";
        }
        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    public static string FormatMessage(string formatText, params object[] args)
    {
        string text = formatText ?? string.Empty;
        List<string> formattedArgs = new List<string>();
        foreach (object arg in args ?? Array.Empty<object>())
        {
            formattedArgs.Add(FormatArgument(arg));
        }

        int argIndex = 0;
        using StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
        for (int index = 0; index < text.Length; ++index)
        {
            char character = text[index];
            if (character == '{')
            {
                if (index + 1 >= text.Length)
                {
                    throw new ArgumentException("unterminated '{' in trace format string");
                }

                char next = text[index + 1];
                if (next == '{')
                {
                    writer.Write('{');
                    index += 1;
                    continue;
                }
                if (next == '}')
                {
                    if (argIndex >= formattedArgs.Count)
                    {
                        throw new ArgumentException("not enough arguments for trace format string");
                    }
                    writer.Write(formattedArgs[argIndex]);
                    argIndex += 1;
                    index += 1;
                    continue;
                }
                throw new ArgumentException("unsupported trace format token");
            }

            if (character == '}')
            {
                if (index + 1 < text.Length && text[index + 1] == '}')
                {
                    writer.Write('}');
                    index += 1;
                    continue;
                }
                throw new ArgumentException("unmatched '}' in trace format string");
            }

            writer.Write(character);
        }

        if (argIndex != formattedArgs.Count)
        {
            throw new ArgumentException("too many arguments for trace format string");
        }
        return writer.ToString();
    }

    internal static string BuildTraceMessagePrefix(Logger logger, string traceNamespace, string channel, SourceContext source)
    {
        OutputOptions options = logger.GetOutputOptions();
        using StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
        if (traceNamespace.Length > 0)
        {
            writer.Write($"[{traceNamespace}] ");
        }
        if (options.Timestamps)
        {
            writer.Write($"[{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.{DateTime.UtcNow:ffffff}] ");
        }
        writer.Write(ColorizeChannel(logger, traceNamespace, channel, $"[{channel}]"));
        if (options.Filenames)
        {
            writer.Write(" ");
            writer.Write(BuildSourceLabel(source, options));
        }
        return writer.ToString();
    }

    internal static string BuildLogMessagePrefix(Logger logger, string traceNamespace, LogSeverity severity, SourceContext source)
    {
        OutputOptions options = logger.GetOutputOptions();
        using StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
        if (traceNamespace.Length > 0)
        {
            writer.Write($"[{traceNamespace}] ");
        }
        if (options.Timestamps)
        {
            writer.Write($"[{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.{DateTime.UtcNow:ffffff}] ");
        }
        writer.Write($"[{SeverityLabel(severity)}]");
        if (options.Filenames)
        {
            writer.Write(" ");
            writer.Write(BuildSourceLabel(source, options));
        }
        return writer.ToString();
    }

    private static string BuildSourceLabel(SourceContext source, OutputOptions options)
    {
        string fileName = string.IsNullOrEmpty(source.FilePath)
            ? "unknown"
            : Path.GetFileNameWithoutExtension(source.FilePath);
        string label = $"[{fileName}";
        if (options.LineNumbers && source.LineNumber > 0)
        {
            label += $":{source.LineNumber}";
        }
        if (options.FunctionNames && !string.IsNullOrEmpty(source.MemberName))
        {
            label += $":{source.MemberName}";
        }
        label += "]";
        return label;
    }

    private static string SeverityLabel(LogSeverity severity)
    {
        return severity switch
        {
            LogSeverity.Info => "info",
            LogSeverity.Warning => "warning",
            LogSeverity.Error => "error",
            _ => "info",
        };
    }

    private static string ColorizeChannel(Logger logger, string traceNamespace, string channel, string text)
    {
        if (Console.IsOutputRedirected || !logger.TryGetColor(traceNamespace, channel, out string colorName))
        {
            return text;
        }

        string code = colorName switch
        {
            "BrightCyan" => "\u001b[96m",
            "BrightYellow" => "\u001b[93m",
            "Gold3" => "\u001b[33m",
            "DeepSkyBlue1" => "\u001b[94m",
            "Orange3" => "\u001b[91m",
            "BrightGreen" => "\u001b[92m",
            "BrightMagenta" => "\u001b[95m",
            _ => string.Empty,
        };
        return code.Length == 0 ? text : $"{code}{text}\u001b[0m";
    }
}

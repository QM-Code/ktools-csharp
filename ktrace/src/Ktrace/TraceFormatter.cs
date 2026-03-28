using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Ktrace;

public static class TraceFormatter
{
    public static readonly string[] ColorNames = TraceColorCatalog.ColorNames;

    private static readonly Dictionary<string, int> ColorIndexes = BuildColorIndexes();

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

        if (!TryGetAnsiColorCode(colorName, out string code))
        {
            return text;
        }

        return $"{code}{text}\u001b[0m";
    }

    private static Dictionary<string, int> BuildColorIndexes()
    {
        Dictionary<string, int> indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int index = 0; index < ColorNames.Length; ++index)
        {
            indexes[ColorNames[index]] = index;
        }
        return indexes;
    }

    private static bool TryGetAnsiColorCode(string colorName, out string code)
    {
        if (!ColorIndexes.TryGetValue(colorName ?? string.Empty, out int index) || index == 0)
        {
            code = string.Empty;
            return false;
        }

        index -= 1;
        if (index <= 7)
        {
            code = $"\u001b[{30 + index}m";
            return true;
        }

        if (index <= 15)
        {
            code = $"\u001b[{90 + (index - 8)}m";
            return true;
        }

        code = $"\u001b[38;5;{index}m";
        return true;
    }
}

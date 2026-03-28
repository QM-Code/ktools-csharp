using System;
using System.Collections.Generic;

namespace Ktrace;

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

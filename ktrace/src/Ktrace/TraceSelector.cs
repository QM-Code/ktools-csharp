using System;
using System.Collections.Generic;

namespace Ktrace;

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

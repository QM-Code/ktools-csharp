using System.Collections.Generic;

namespace Kcli;

internal static partial class ParseEngine
{
    private static List<string> BuildParseTokens(int argc, string[] argv)
    {
        List<string> tokens = new List<string>(argc);
        for (int index = 0; index < argc; ++index)
        {
            tokens.Add(argv[index] ?? string.Empty);
        }
        return tokens;
    }

    private static bool IsCollectableFollowOnValueToken(string value)
    {
        return value.Length == 0 || value[0] != '-';
    }

    private static CollectedValues CollectValueTokens(
        int optionIndex,
        List<string> tokens,
        bool[] consumed,
        bool allowOptionLikeFirstValue)
    {
        CollectedValues collected = new CollectedValues
        {
            LastIndex = optionIndex,
        };

        int firstValueIndex = optionIndex + 1;
        bool hasNext = firstValueIndex >= 0 &&
            firstValueIndex < tokens.Count &&
            !consumed[firstValueIndex];
        if (!hasNext)
        {
            return collected;
        }

        string first = tokens[firstValueIndex];
        if (!allowOptionLikeFirstValue && first.Length > 0 && first[0] == '-')
        {
            return collected;
        }

        collected.HasValue = true;
        collected.Parts.Add(first);
        consumed[firstValueIndex] = true;
        collected.LastIndex = firstValueIndex;

        if (allowOptionLikeFirstValue && first.Length > 0 && first[0] == '-')
        {
            return collected;
        }

        for (int scan = firstValueIndex + 1; scan < tokens.Count; ++scan)
        {
            if (consumed[scan])
            {
                continue;
            }

            string next = tokens[scan];
            if (!IsCollectableFollowOnValueToken(next))
            {
                break;
            }

            collected.Parts.Add(next);
            consumed[scan] = true;
            collected.LastIndex = scan;
        }

        return collected;
    }

    private static void ConsumeIndex(bool[] consumed, int index)
    {
        if (index >= 0 && index < consumed.Length && !consumed[index])
        {
            consumed[index] = true;
        }
    }

    private static bool HasAliasPresetTokens(AliasBinding aliasBinding)
    {
        return aliasBinding != null && aliasBinding.PresetTokens.Count > 0;
    }

    private static List<string> BuildEffectiveValueTokens(AliasBinding aliasBinding, List<string> collectedParts)
    {
        List<string> merged = new List<string>();
        if (HasAliasPresetTokens(aliasBinding))
        {
            merged.AddRange(aliasBinding.PresetTokens);
        }

        merged.AddRange(collectedParts);
        return merged;
    }
}

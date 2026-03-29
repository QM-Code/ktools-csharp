using System.Collections.Generic;

namespace Kcli;

internal static partial class ParseEngine
{
    private static int ScheduleInvocation(
        CommandBinding binding,
        AliasBinding aliasBinding,
        string root,
        string command,
        string optionToken,
        int index,
        List<string> tokens,
        bool[] consumed,
        List<Invocation> invocations,
        MutableParseOutcome result)
    {
        ConsumeIndex(consumed, index);

        Invocation invocation = new Invocation
        {
            Root = root,
            Option = optionToken,
            Command = command,
        };

        if (!binding.ExpectsValue)
        {
            if (HasAliasPresetTokens(aliasBinding))
            {
                result.ReportError(
                    aliasBinding.Alias,
                    $"alias '{aliasBinding.Alias}' presets values for option '{optionToken}' which does not accept values");
                return index;
            }

            invocation.Kind = InvocationKind.Flag;
            invocation.FlagHandler = binding.FlagHandler;
            invocations.Add(invocation);
            return index;
        }

        CollectedValues collected = CollectValueTokens(index, tokens, consumed, binding.ValueArity == ValueArity.Required);
        if (!collected.HasValue && !HasAliasPresetTokens(aliasBinding) && binding.ValueArity == ValueArity.Required)
        {
            result.ReportError(optionToken, $"option '{optionToken}' requires a value");
            return index;
        }

        if (collected.HasValue)
        {
            index = collected.LastIndex;
        }

        invocation.Kind = InvocationKind.Value;
        invocation.ValueHandler = binding.ValueHandler;
        invocation.ValueTokens.AddRange(BuildEffectiveValueTokens(aliasBinding, collected.Parts));
        invocations.Add(invocation);
        return index;
    }

    private static void SchedulePositionals(
        ParserData data,
        List<string> tokens,
        bool[] consumed,
        List<Invocation> invocations)
    {
        if (data.PositionalHandler == null || tokens.Count <= 1)
        {
            return;
        }

        Invocation invocation = new Invocation
        {
            Kind = InvocationKind.Positional,
            PositionalHandler = data.PositionalHandler,
        };

        for (int index = 1; index < tokens.Count; ++index)
        {
            if (consumed[index])
            {
                continue;
            }

            string token = tokens[index];
            if (token.Length == 0 || token[0] != '-')
            {
                consumed[index] = true;
                invocation.ValueTokens.Add(token);
            }
        }

        if (invocation.ValueTokens.Count > 0)
        {
            invocations.Add(invocation);
        }
    }
}

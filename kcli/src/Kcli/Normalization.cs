using System;

namespace Kcli;

internal static class Normalization
{
    public static bool StartsWith(string value, string prefix)
    {
        return value.StartsWith(prefix, StringComparison.Ordinal);
    }

    public static string TrimWhitespace(string value)
    {
        return (value ?? string.Empty).Trim();
    }

    public static bool ContainsWhitespace(string value)
    {
        foreach (char character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                return true;
            }
        }

        return false;
    }

    public static string NormalizeRootNameOrThrow(string rawRoot)
    {
        string root = TrimWhitespace(rawRoot);
        if (root.Length == 0)
        {
            throw new ArgumentException("kcli root must not be empty");
        }
        if (root[0] == '-')
        {
            throw new ArgumentException("kcli root must not begin with '-'");
        }
        if (ContainsWhitespace(root))
        {
            throw new ArgumentException("kcli root is invalid");
        }
        return root;
    }

    public static string NormalizeInlineRootOptionOrThrow(string rawRoot)
    {
        string root = TrimWhitespace(rawRoot);
        if (root.Length == 0)
        {
            throw new ArgumentException("kcli root must not be empty");
        }
        if (StartsWith(root, "--"))
        {
            root = root.Substring(2);
        }
        else if (root[0] == '-')
        {
            throw new ArgumentException("kcli root must use '--root' or 'root'");
        }
        return NormalizeRootNameOrThrow(root);
    }

    public static string NormalizeInlineHandlerOptionOrThrow(string rawOption, string rootName)
    {
        string option = TrimWhitespace(rawOption);
        if (option.Length == 0)
        {
            throw new ArgumentException("kcli inline handler option must not be empty");
        }

        if (StartsWith(option, "--"))
        {
            string fullPrefix = $"--{rootName}-";
            if (!StartsWith(option, fullPrefix))
            {
                throw new ArgumentException($"kcli inline handler option must use '-name' or '{fullPrefix}name'");
            }
            option = option.Substring(fullPrefix.Length);
        }
        else if (option[0] == '-')
        {
            option = option.Substring(1);
        }
        else
        {
            throw new ArgumentException($"kcli inline handler option must use '-name' or '--{rootName}-name'");
        }

        return NormalizeCommandOrThrow(option);
    }

    public static string NormalizePrimaryHandlerOptionOrThrow(string rawOption)
    {
        string option = TrimWhitespace(rawOption);
        if (option.Length == 0)
        {
            throw new ArgumentException("kcli end-user handler option must not be empty");
        }

        if (StartsWith(option, "--"))
        {
            option = option.Substring(2);
        }
        else if (option[0] == '-')
        {
            throw new ArgumentException("kcli end-user handler option must use '--name' or 'name'");
        }

        return NormalizeCommandOrThrow(option);
    }

    public static string NormalizeAliasOrThrow(string rawAlias)
    {
        string alias = TrimWhitespace(rawAlias);
        if (alias.Length < 2 || alias[0] != '-' || StartsWith(alias, "--") || ContainsWhitespace(alias))
        {
            throw new ArgumentException("kcli alias must use single-dash form, e.g. '-v'");
        }
        return alias;
    }

    public static string NormalizeAliasTargetOptionOrThrow(string rawTarget)
    {
        string target = TrimWhitespace(rawTarget);
        if (target.Length < 3 || !StartsWith(target, "--") || ContainsWhitespace(target) || target[2] == '-')
        {
            throw new ArgumentException("kcli alias target must use double-dash form, e.g. '--verbose'");
        }
        return target;
    }

    public static string NormalizeHelpPlaceholderOrThrow(string rawPlaceholder)
    {
        string placeholder = TrimWhitespace(rawPlaceholder);
        if (placeholder.Length == 0)
        {
            throw new ArgumentException("kcli help placeholder must not be empty");
        }
        return placeholder;
    }

    public static string NormalizeDescriptionOrThrow(string rawDescription)
    {
        string description = TrimWhitespace(rawDescription);
        if (description.Length == 0)
        {
            throw new ArgumentException("kcli command description must not be empty");
        }
        return description;
    }

    public static void ThrowCliError(MutableParseOutcome result)
    {
        if (result.Ok)
        {
            throw new InvalidOperationException("kcli internal error: ThrowCliError called without a failure");
        }

        throw new CliError(result.ErrorOption, result.ErrorMessage);
    }

    private static string NormalizeCommandOrThrow(string command)
    {
        if (command.Length == 0)
        {
            throw new ArgumentException("kcli command must not be empty");
        }
        if (command[0] == '-')
        {
            throw new ArgumentException("kcli command must not start with '-'");
        }
        if (ContainsWhitespace(command))
        {
            throw new ArgumentException("kcli command must not contain whitespace");
        }
        return command;
    }
}

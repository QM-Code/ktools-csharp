using System;

namespace Kcli;

public sealed class Parser
{
    private readonly ParserData _data = new ParserData();

    public void AddAlias(string alias, string target)
    {
        Registration.SetAlias(_data, alias, target, Array.Empty<string>());
    }

    public void AddAlias(string alias, string target, params string[] presetTokens)
    {
        Registration.SetAlias(_data, alias, target, presetTokens ?? Array.Empty<string>());
    }

    public void SetHandler(string option, FlagHandler handler, string description)
    {
        Registration.SetPrimaryHandler(_data, option, handler, description);
    }

    public void SetHandler(string option, ValueHandler handler, string description)
    {
        Registration.SetPrimaryHandler(_data, option, handler, description);
    }

    public void SetOptionalValueHandler(string option, ValueHandler handler, string description)
    {
        Registration.SetPrimaryOptionalValueHandler(_data, option, handler, description);
    }

    public void SetPositionalHandler(PositionalHandler handler)
    {
        Registration.SetPositionalHandler(_data, handler);
    }

    public void AddInlineParser(InlineParser parser)
    {
        if (parser == null)
        {
            throw new ArgumentNullException(nameof(parser), "kcli inline parser must not be empty");
        }

        Registration.AddInlineParser(_data, parser.Snapshot());
    }

    public void ParseOrExit(int argc, string[] argv)
    {
        try
        {
            ParseEngine.Parse(_data, argc, argv);
        }
        catch (CliError ex)
        {
            CliConsole.ReportCliErrorAndExit(ex.Message);
        }
    }

    public void ParseOrThrow(int argc, string[] argv)
    {
        ParseEngine.Parse(_data, argc, argv);
    }

    public void ParseOrExit(string[] args)
    {
        string[] argv = WithProgramToken(args);
        ParseOrExit(argv.Length, argv);
    }

    public void ParseOrThrow(string[] args)
    {
        string[] argv = WithProgramToken(args);
        ParseOrThrow(argv.Length, argv);
    }

    private static string[] WithProgramToken(string[] args)
    {
        if (args == null)
        {
            return Array.Empty<string>();
        }

        string[] argv = new string[args.Length + 1];
        argv[0] = string.Empty;
        Array.Copy(args, 0, argv, 1, args.Length);
        return argv;
    }
}

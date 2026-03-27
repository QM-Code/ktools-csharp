using System;
using System.Collections.Generic;
using System.IO;
using Kcli;

namespace Kcli.Tests;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            ApiTests.Run();
            Console.WriteLine("C# kcli tests passed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }
}

internal static class ApiTests
{
    public static void Run()
    {
        TestParserEmptyParseSucceeds();
        TestUnknownOptionDoesNotRunHandlers();
        TestAliasRewritesOption();
        TestAliasPresetTokensSatisfyRequiredValue();
        TestAliasPresetTokensApplyToInlineRootValue();
        TestAliasPresetTokensRejectedForFlags();
        TestRequiredValueAcceptsOptionLikeFirstToken();
        TestBareInlineRootPrintsHelp();
        TestDoubleDashRemainsUnknown();
    }

    private static void TestParserEmptyParseSucceeds()
    {
        string[] argv = { "prog" };
        Parser parser = new Parser();
        parser.ParseOrThrow(argv.Length, argv);
        Assert.Equal(string.Join("|", argv), "prog", "parseOrThrow should leave argv unchanged");
    }

    private static void TestUnknownOptionDoesNotRunHandlers()
    {
        string[] argv = { "prog", "--verbose", "pos1", "--output", "stdout", "--bogus" };
        bool verbose = false;
        string output = string.Empty;
        List<string> positionals = new List<string>();

        Parser parser = new Parser();
        parser.SetHandler("--verbose", _ => verbose = true, "Enable verbose logging.");
        parser.SetHandler("--output", (_, value) => output = value, "Set output target.");
        parser.SetPositionalHandler(context => positionals.AddRange(context.ValueTokens));

        CliError error = Assert.Throws<CliError>(() => parser.ParseOrThrow(argv.Length, argv), "unknown option should fail before handlers run");
        Assert.True(!verbose, "verbose handler should not have run");
        Assert.Equal(output, string.Empty, "value handler should not have run");
        Assert.Equal(positionals.Count, 0, "positional handler should not have run");
        Assert.Equal(error.Option, "--bogus", "CliError option should match unknown token");
        Assert.Contains(error.Message, "unknown option --bogus", "CliError should describe unknown option");
    }

    private static void TestAliasRewritesOption()
    {
        string seen = string.Empty;
        Parser parser = new Parser();
        parser.AddAlias("-v", "--verbose");
        parser.SetHandler("--verbose", context => seen = context.Option, "Enable verbose logging.");
        parser.ParseOrThrow(new[] { "-v" });
        Assert.Equal(seen, "--verbose", "alias should rewrite the effective option");
    }

    private static void TestAliasPresetTokensSatisfyRequiredValue()
    {
        string seen = string.Empty;
        List<string> tokens = new List<string>();
        Parser parser = new Parser();
        parser.AddAlias("-p", "--profile", "release");
        parser.SetHandler("--profile", (context, value) =>
        {
            seen = value;
            tokens.AddRange(context.ValueTokens);
        }, "Set active profile.");
        parser.ParseOrThrow(new[] { "-p" });
        Assert.Equal(seen, "release", "preset token should satisfy required value");
        Assert.Equal(string.Join("|", tokens), "release", "context tokens should include preset value");
    }

    private static void TestAliasPresetTokensApplyToInlineRootValue()
    {
        string option = string.Empty;
        string value = string.Empty;

        Parser parser = new Parser();
        InlineParser config = new InlineParser("--config");
        config.SetRootValueHandler((context, captured) =>
        {
            option = context.Option;
            value = captured;
        }, "<assignment>", "Store a config assignment.");
        parser.AddInlineParser(config);
        parser.AddAlias("-c", "--config", "user-file=/tmp/user.json");
        parser.ParseOrThrow(new[] { "-c" });

        Assert.Equal(option, "--config", "inline root option should be preserved");
        Assert.Equal(value, "user-file=/tmp/user.json", "preset value should reach root handler");
    }

    private static void TestAliasPresetTokensRejectedForFlags()
    {
        Parser parser = new Parser();
        parser.AddAlias("-v", "--verbose", "unexpected");
        parser.SetHandler("--verbose", _ => { }, "Enable verbose logging.");

        CliError error = Assert.Throws<CliError>(() => parser.ParseOrThrow(new[] { "-v" }), "flag aliases must reject preset tokens");
        Assert.Equal(error.Option, "-v", "error should surface the alias token");
        Assert.Contains(error.Message, "does not accept values", "error should explain the flag rejection");
    }

    private static void TestRequiredValueAcceptsOptionLikeFirstToken()
    {
        string captured = string.Empty;
        Parser parser = new Parser();
        parser.AddAlias("-v", "--verbose");
        parser.SetHandler("--output", (_, value) => captured = value, "Set output target.");
        parser.SetHandler("--verbose", _ => throw new InvalidOperationException("verbose should not be treated as a separate option"), "Enable verbose logging.");
        parser.ParseOrThrow(new[] { "--output", "-v" });
        Assert.Equal(captured, "-v", "required values should accept option-like first tokens");
    }

    private static void TestBareInlineRootPrintsHelp()
    {
        Parser parser = new Parser();
        InlineParser alpha = new InlineParser("--alpha");
        alpha.SetOptionalValueHandler("-enable", (_, _) => { }, "Enable alpha processing.");
        parser.AddInlineParser(alpha);

        string stdout = CaptureStdout(() => parser.ParseOrThrow(new[] { "--alpha" }));
        Assert.Contains(stdout, "Available --alpha-* options:", "bare inline root should print help");
        Assert.Contains(stdout, "--alpha-enable [value]", "help should include optional value syntax");
    }

    private static void TestDoubleDashRemainsUnknown()
    {
        Parser parser = new Parser();
        parser.AddAlias("-v", "--verbose");
        parser.SetHandler("--verbose", _ => { }, "Enable verbose logging.");
        CliError error = Assert.Throws<CliError>(() => parser.ParseOrThrow(new[] { "--", "-v" }), "double dash should remain an unknown option");
        Assert.Equal(error.Option, "--", "double dash should be reported as the failing option");
    }

    private static string CaptureStdout(Action action)
    {
        StringWriter writer = new StringWriter();
        TextWriter previous = Console.Out;
        try
        {
            Console.SetOut(writer);
            action();
            return writer.ToString();
        }
        finally
        {
            Console.SetOut(previous);
        }
    }
}

internal static class Assert
{
    public static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void Equal<T>(T actual, T expected, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(actual, expected))
        {
            throw new InvalidOperationException($"{message}\nexpected: {expected}\nactual:   {actual}");
        }
    }

    public static void Contains(string actual, string needle, string message)
    {
        if ((actual ?? string.Empty).Contains(needle, StringComparison.Ordinal))
        {
            return;
        }
        throw new InvalidOperationException($"{message}\nmissing: {needle}\nactual:  {actual}");
    }

    public static T Throws<T>(Action action, string message)
        where T : Exception
    {
        try
        {
            action();
        }
        catch (T ex)
        {
            return ex;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"{message}\nexpected: {typeof(T).Name}\nactual:   {ex.GetType().Name}");
        }

        throw new InvalidOperationException($"{message}\nexpected: {typeof(T).Name}\nactual:   none");
    }
}

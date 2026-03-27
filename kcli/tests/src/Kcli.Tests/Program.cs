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
        TestRootValueHandlerHelpRowPrints();
        TestInlineRootValueHandlerJoinsTokens();
        TestOptionalValueHandlerAcceptsExplicitEmptyValue();
        TestParserCanBeReusedAcrossParses();
        TestDuplicateInlineRootRejected();
        TestOptionHandlerExceptionThrowsCliError();
        TestPositionalHandlerExceptionThrowsCliError();
        TestPositionalHandlerPreservesExplicitEmptyTokens();
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

    private static void TestRootValueHandlerHelpRowPrints()
    {
        Parser parser = new Parser();
        InlineParser build = new InlineParser("--build");
        build.SetRootValueHandler((_, _) => { }, "<selector>", "Select build targets.");
        parser.AddInlineParser(build);

        string stdout = CaptureStdout(() => parser.ParseOrThrow(new[] { "--build" }));
        Assert.Contains(stdout, "--build <selector>", "bare root help should include the root value placeholder");
        Assert.Contains(stdout, "Select build targets.", "bare root help should include the root value description");
    }

    private static void TestInlineRootValueHandlerJoinsTokens()
    {
        string joined = string.Empty;
        List<string> tokens = new List<string>();

        Parser parser = new Parser();
        InlineParser config = new InlineParser("--config");
        config.SetRootValueHandler((context, value) =>
        {
            joined = value;
            tokens.AddRange(context.ValueTokens);
        }, "<assignment>", "Store a config assignment.");
        parser.AddInlineParser(config);
        parser.ParseOrThrow(new[] { "--config", "user=alice", "profile=prod" });

        Assert.Equal(joined, "user=alice profile=prod", "inline root values should be joined with spaces");
        Assert.Equal(string.Join("|", tokens), "user=alice|profile=prod", "inline root handlers should receive all value tokens");
    }

    private static void TestOptionalValueHandlerAcceptsExplicitEmptyValue()
    {
        string captured = "unset";
        List<string> tokens = new List<string>();

        Parser parser = new Parser();
        parser.SetOptionalValueHandler("--color", (context, value) =>
        {
            captured = value;
            tokens.AddRange(context.ValueTokens);
        }, "Set or auto-detect color output.");
        parser.ParseOrThrow(new[] { "--color", string.Empty });

        Assert.Equal(captured, string.Empty, "optional value handlers should accept explicit empty values");
        Assert.Equal(tokens.Count, 1, "explicit empty values should still count as a value token");
        Assert.Equal(tokens[0], string.Empty, "explicit empty tokens should be preserved");
    }

    private static void TestParserCanBeReusedAcrossParses()
    {
        List<string> outputs = new List<string>();
        Parser parser = new Parser();
        parser.SetHandler("--output", (_, value) => outputs.Add(value), "Set output target.");

        parser.ParseOrThrow(new[] { "--output", "stdout" });
        parser.ParseOrThrow(new[] { "--output", "stderr" });

        Assert.Equal(string.Join("|", outputs), "stdout|stderr", "parser instances should be reusable across parses");
    }

    private static void TestDuplicateInlineRootRejected()
    {
        Parser parser = new Parser();
        parser.AddInlineParser(new InlineParser("--build"));

        ArgumentException error = Assert.Throws<ArgumentException>(() =>
        {
            parser.AddInlineParser(new InlineParser("build"));
        }, "duplicate inline parser roots should be rejected");

        Assert.Contains(error.Message, "already registered", "duplicate root errors should explain the conflict");
    }

    private static void TestOptionHandlerExceptionThrowsCliError()
    {
        Parser parser = new Parser();
        parser.SetHandler("--boom", _ => throw new InvalidOperationException("handler failed"), "Trigger failure.");

        CliError error = Assert.Throws<CliError>(() => parser.ParseOrThrow(new[] { "--boom" }), "handler failures should surface as CliError");
        Assert.Equal(error.Option, "--boom", "CliError should surface the failing option");
        Assert.Contains(error.Message, "option '--boom': handler failed", "CliError should preserve the handler failure message");
    }

    private static void TestPositionalHandlerExceptionThrowsCliError()
    {
        Parser parser = new Parser();
        parser.SetPositionalHandler(_ => throw new InvalidOperationException("positionals failed"));

        CliError error = Assert.Throws<CliError>(() => parser.ParseOrThrow(new[] { "input.txt" }), "positional failures should surface as CliError");
        Assert.Equal(error.Option, string.Empty, "positional handler failures should not report an option token");
        Assert.Contains(error.Message, "positionals failed", "positional handler failures should preserve the original message");
    }

    private static void TestPositionalHandlerPreservesExplicitEmptyTokens()
    {
        List<string> tokens = new List<string>();
        Parser parser = new Parser();
        parser.SetPositionalHandler(context => tokens.AddRange(context.ValueTokens));
        parser.ParseOrThrow(new[] { "first", string.Empty, "last" });

        Assert.Equal(string.Join("|", tokens), "first||last", "positional handlers should preserve explicit empty tokens");
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

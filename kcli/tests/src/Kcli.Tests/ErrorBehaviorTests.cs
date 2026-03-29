using System;
using System.Collections.Generic;
using Ktools.CSharp.Tests;

namespace Kcli.Tests;

internal static class ErrorBehaviorTests
{
    public static void Run()
    {
        TestUnknownOptionDoesNotRunHandlers();
        TestOptionHandlerExceptionThrowsCliError();
        TestPositionalHandlerExceptionThrowsCliError();
        TestDoubleDashRemainsUnknown();
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

        CliError error = TestAssert.Throws<CliError>(() => parser.ParseOrThrow(argv.Length, argv), "unknown option should fail before handlers run");
        TestAssert.True(!verbose, "verbose handler should not have run");
        TestAssert.Equal(output, string.Empty, "value handler should not have run");
        TestAssert.Equal(positionals.Count, 0, "positional handler should not have run");
        TestAssert.Equal(error.Option, "--bogus", "CliError option should match unknown token");
        TestAssert.Contains(error.Message, "unknown option --bogus", "CliError should describe unknown option");
    }

    private static void TestOptionHandlerExceptionThrowsCliError()
    {
        Parser parser = new Parser();
        parser.SetHandler("--boom", _ => throw new InvalidOperationException("handler failed"), "Trigger failure.");

        CliError error = TestAssert.Throws<CliError>(() => parser.ParseOrThrow(new[] { "--boom" }), "handler failures should surface as CliError");
        TestAssert.Equal(error.Option, "--boom", "CliError should surface the failing option");
        TestAssert.Contains(error.Message, "option '--boom': handler failed", "CliError should preserve the handler failure message");
    }

    private static void TestPositionalHandlerExceptionThrowsCliError()
    {
        Parser parser = new Parser();
        parser.SetPositionalHandler(_ => throw new InvalidOperationException("positionals failed"));

        CliError error = TestAssert.Throws<CliError>(() => parser.ParseOrThrow(new[] { "input.txt" }), "positional failures should surface as CliError");
        TestAssert.Equal(error.Option, string.Empty, "positional handler failures should not report an option token");
        TestAssert.Contains(error.Message, "positionals failed", "positional handler failures should preserve the original message");
    }

    private static void TestDoubleDashRemainsUnknown()
    {
        Parser parser = new Parser();
        parser.AddAlias("-v", "--verbose");
        parser.SetHandler("--verbose", _ => { }, "Enable verbose logging.");
        CliError error = TestAssert.Throws<CliError>(() => parser.ParseOrThrow(new[] { "--", "-v" }), "double dash should remain an unknown option");
        TestAssert.Equal(error.Option, "--", "double dash should be reported as the failing option");
    }
}

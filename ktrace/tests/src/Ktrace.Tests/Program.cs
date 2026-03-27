using System;
using System.Collections.Generic;
using System.IO;
using Ktrace;

namespace Ktrace.Tests;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            FormatTests.Run();
            ChannelTests.Run();
            Console.WriteLine("C# ktrace tests passed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }
}

internal static class FormatTests
{
    public static void Run()
    {
        Assert.Equal(TraceFormatter.FormatMessage("value {} {}", 7, "done"), "value 7 done", "format placeholders should be substituted");
        Assert.Equal(TraceFormatter.FormatMessage("escaped {{}}"), "escaped {}", "escaped braces should be preserved");
        Assert.Equal(TraceFormatter.FormatMessage("bool {}", true), "bool true", "bools should format lower-case");

        Assert.Throws<ArgumentException>(() => TraceFormatter.FormatMessage("value {} {}", 7), "missing arguments should fail");
        Assert.Throws<ArgumentException>(() => TraceFormatter.FormatMessage("{"), "unterminated braces should fail");

        string output = CaptureStdout(() =>
        {
            Logger logger = new Logger();
            TraceLogger trace = new TraceLogger("tests");
            logger.AddTraceLogger(trace);
            trace.Warn("escaped {{}} {}", 7);
        });
        Assert.Contains(output, "escaped {} 7", "warn output should contain formatted text");
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

internal static class ChannelTests
{
    public static void Run()
    {
        VerifyExplicitOnOffSemantics();
        VerifyRegisteredChannelSemantics();
        VerifyLogPrefixes();
    }

    private static void VerifyExplicitOnOffSemantics()
    {
        Logger logger = new Logger();
        AddTestChannels(logger);

        logger.EnableChannels("tests.*");
        Assert.True(logger.ShouldTraceChannel("tests.net"), "tests.net should be enabled by tests.*");
        Assert.True(logger.ShouldTraceChannel("tests.cache"), "tests.cache should be enabled by tests.*");

        logger.DisableChannels("tests.*");
        Assert.True(!logger.ShouldTraceChannel("tests.net"), "tests.net should be disabled after tests.* disable");
        Assert.True(!logger.ShouldTraceChannel("tests.cache"), "tests.cache should be disabled after tests.* disable");

        logger.EnableChannel("tests.net");
        Assert.True(logger.ShouldTraceChannel("tests.net"), "explicit enable should turn tests.net back on");
        Assert.True(!logger.ShouldTraceChannel("tests.cache"), "tests.cache should stay off after explicit enable");

        logger.DisableChannel("tests.net");
        Assert.True(!logger.ShouldTraceChannel("tests.net"), "explicit disable should turn tests.net back off");
    }

    private static void VerifyRegisteredChannelSemantics()
    {
        Logger logger = new Logger();
        AddTestChannels(logger);

        logger.EnableChannels("*.*.*");
        Assert.True(logger.ShouldTraceChannel("tests.store.requests"), "tests.store.requests should trace when explicitly registered and enabled");
        Assert.True(logger.ShouldTraceChannel("tests.net"), "tests.net should trace when *.*.* enables channels up to depth 2");
        Assert.True(!logger.ShouldTraceChannel("tests.bad name"), "invalid runtime channel names should not trace");

        logger.EnableChannel("tests.missing.child");
        Assert.True(!logger.ShouldTraceChannel("tests.missing.child"), "enableChannel should ignore unregistered exact channels");

        logger.EnableChannels("tests.missing.child");
        Assert.True(!logger.ShouldTraceChannel("tests.missing.child"), "enableChannels should ignore unresolved exact selectors");
    }

    private static void VerifyLogPrefixes()
    {
        string output = CaptureStdout(() =>
        {
            Logger logger = new Logger();
            TraceLogger trace = new TraceLogger("tests");
            logger.AddTraceLogger(trace);
            logger.SetOutputOptions(new OutputOptions
            {
                Filenames = true,
                LineNumbers = true,
            });
            trace.Info("info message");
            trace.Warn("warn value {}", 7);
            trace.Error("error message");
        });

        Assert.Contains(output, "[tests] [info]", "info prefix should include namespace and severity");
        Assert.Contains(output, "[tests] [warning]", "warning prefix should include namespace and severity");
        Assert.Contains(output, "[tests] [error]", "error prefix should include namespace and severity");
    }

    private static void AddTestChannels(Logger logger)
    {
        TraceLogger tracer = new TraceLogger("tests");
        tracer.AddChannel("net");
        tracer.AddChannel("cache");
        tracer.AddChannel("store");
        tracer.AddChannel("store.requests");
        logger.AddTraceLogger(tracer);
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

    public static void Equal(string actual, string expected, string message)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
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

    public static void Throws<T>(Action action, string message)
        where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"{message}\nexpected: {typeof(T).Name}\nactual:   {ex.GetType().Name}");
        }

        throw new InvalidOperationException($"{message}\nexpected: {typeof(T).Name}\nactual:   none");
    }
}

using System;
using System.Collections.Generic;
using System.IO;

namespace Ktools.CSharp.Tests;

public static class TestConsole
{
    public static string CaptureStdout(Action action)
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

public static class TestAssert
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

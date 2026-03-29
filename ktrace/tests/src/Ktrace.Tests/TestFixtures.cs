using System;
using System.Reflection;
using Ktools.CSharp.Tests;

namespace Ktrace.Tests;

internal static class TestFixtures
{
    public static void AddTestChannels(Logger logger)
    {
        TraceLogger tracer = new TraceLogger("tests");
        tracer.AddChannel("net");
        tracer.AddChannel("cache");
        tracer.AddChannel("store");
        tracer.AddChannel("store.requests");
        logger.AddTraceLogger(tracer);
    }

    public static string ResolveChannelColor(Logger logger, string traceNamespace, string channel)
    {
        MethodInfo tryGetColor = typeof(Logger).GetMethod("TryGetColor", BindingFlags.Instance | BindingFlags.NonPublic);
        if (tryGetColor == null)
        {
            throw new InvalidOperationException("failed to locate Logger.TryGetColor via reflection");
        }

        object[] args = { traceNamespace, channel, null };
        bool found = (bool)(tryGetColor.Invoke(logger, args) ?? false);
        TestAssert.True(found, "expected channel color lookup to succeed");
        return args[2] as string ?? string.Empty;
    }

    public static int NextSourceLine()
    {
        return new System.Diagnostics.StackFrame(1, true).GetFileLineNumber() + 1;
    }

    public static int CountOccurrences(string text, string needle)
    {
        int count = 0;
        int index = 0;
        while (true)
        {
            index = (text ?? string.Empty).IndexOf(needle, index, System.StringComparison.Ordinal);
            if (index < 0)
            {
                return count;
            }

            count += 1;
            index += needle.Length;
        }
    }
}

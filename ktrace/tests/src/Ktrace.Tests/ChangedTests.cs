using System;
using System.Collections.Generic;
using System.Threading;
using Ktools.CSharp.Tests;

namespace Ktrace.Tests;

internal static class ChangedTests
{
    public static void Run()
    {
        VerifyChangedPerTraceLogger();
        VerifyChangedThreadSafety();
    }

    private static void VerifyChangedPerTraceLogger()
    {
        Logger logger = new Logger();

        TraceLogger first = new TraceLogger("alpha");
        first.AddChannel("changed");
        logger.AddTraceLogger(first);
        logger.EnableChannel("alpha.changed");

        TraceLogger second = new TraceLogger("beta");
        second.AddChannel("changed");
        logger.AddTraceLogger(second);
        logger.EnableChannel("beta.changed");

        string output = TestConsole.CaptureStdout(() =>
        {
            EmitChanged(first);
            EmitChanged(first);
            EmitChanged(second);
            EmitChanged(second);
        });

        TestAssert.Equal(TestFixtures.CountOccurrences(output, "first changed"), 1, "traceChanged should suppress duplicates for the same trace logger");
        TestAssert.Equal(TestFixtures.CountOccurrences(output, "second changed"), 1, "traceChanged should keep duplicate state per trace logger");
    }

    private static void VerifyChangedThreadSafety()
    {
        List<Exception> errors = new List<Exception>();
        TestConsole.CaptureStdout(() =>
        {
            Logger logger = new Logger();
            TraceLogger trace = new TraceLogger("tests");
            trace.AddChannel("changed");
            logger.AddTraceLogger(trace);
            logger.EnableChannel("tests.changed");

            const int threadCount = 8;
            const int iterationsPerThread = 4000;
            int readyThreads = 0;
            ManualResetEventSlim start = new ManualResetEventSlim(false);
            List<Thread> workers = new List<Thread>();

            for (int threadIndex = 0; threadIndex < threadCount; ++threadIndex)
            {
                int capturedIndex = threadIndex;
                Thread worker = new Thread(() =>
                {
                    try
                    {
                        Interlocked.Increment(ref readyThreads);
                        start.Wait();
                        for (int iteration = 0; iteration < iterationsPerThread; ++iteration)
                        {
                            trace.TraceChanged("changed", $"{capturedIndex}:{iteration & 1}", "changed");
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (errors)
                        {
                            errors.Add(ex);
                        }
                    }
                });

                worker.Start();
                workers.Add(worker);
            }

            while (Volatile.Read(ref readyThreads) < threadCount)
            {
                Thread.Yield();
            }

            start.Set();
            foreach (Thread worker in workers)
            {
                worker.Join();
            }
        });

        TestAssert.Equal(errors.Count, 0, "traceChanged should stay thread-safe under concurrent use");
    }

    private static void EmitChanged(TraceLogger trace)
    {
        trace.TraceChanged("changed", "stable-key", trace.Namespace == "alpha" ? "first changed" : "second changed");
    }
}

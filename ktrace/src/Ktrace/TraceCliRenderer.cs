using System;
using System.Collections.Generic;

namespace Ktrace;

internal static class TraceCliRenderer
{
    public static void WriteNamespaces(IReadOnlyList<string> namespaces)
    {
        if (namespaces.Count == 0)
        {
            Console.WriteLine("No trace namespaces defined.");
            Console.WriteLine();
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Available trace namespaces:");
        foreach (string traceNamespace in namespaces)
        {
            Console.WriteLine($"  {traceNamespace}");
        }
        Console.WriteLine();
    }

    public static void WriteChannels(Logger logger)
    {
        bool printedAny = false;
        foreach (string traceNamespace in logger.GetNamespaces())
        {
            foreach (string channel in logger.GetChannels(traceNamespace))
            {
                if (!printedAny)
                {
                    Console.WriteLine();
                    Console.WriteLine("Available trace channels:");
                    printedAny = true;
                }

                Console.WriteLine($"  {traceNamespace}.{channel}");
            }
        }

        if (!printedAny)
        {
            Console.WriteLine("No trace channels defined.");
            Console.WriteLine();
            return;
        }

        Console.WriteLine();
    }

    public static void WriteColors()
    {
        Console.WriteLine();
        Console.WriteLine("Available trace colors:");
        foreach (string color in TraceFormatter.ColorNames)
        {
            Console.WriteLine($"  {color}");
        }
        Console.WriteLine();
    }

    public static void WriteExamples(string root)
    {
        string optionRoot = $"--{root}";
        Console.WriteLine();
        Console.WriteLine("General trace selector pattern:");
        Console.WriteLine($"  {optionRoot} <namespace>.<channel>[.<subchannel>[.<subchannel>]]");
        Console.WriteLine();
        Console.WriteLine("Trace selector examples:");
        Console.WriteLine($"  {optionRoot} '.abc'           Select local 'abc' in current namespace");
        Console.WriteLine($"  {optionRoot} '.abc.xyz'       Select local nested channel in current namespace");
        Console.WriteLine($"  {optionRoot} 'otherapp.channel' Select explicit namespace channel");
        Console.WriteLine($"  {optionRoot} '*.*'            Select all <namespace>.<channel> channels");
        Console.WriteLine($"  {optionRoot} '*.*.*'          Select all channels up to 2 levels");
        Console.WriteLine($"  {optionRoot} '*.*.*.*'        Select all channels up to 3 levels");
        Console.WriteLine($"  {optionRoot} 'alpha.*'        Select all top-level channels in alpha");
        Console.WriteLine($"  {optionRoot} 'alpha.*.*'      Select all channels in alpha (up to 2 levels)");
        Console.WriteLine($"  {optionRoot} 'alpha.*.*.*'    Select all channels in alpha (up to 3 levels)");
        Console.WriteLine($"  {optionRoot} '*.net'          Select 'net' across all namespaces");
        Console.WriteLine($"  {optionRoot} '*.scheduler.tick' Select 'scheduler.tick' across namespaces");
        Console.WriteLine($"  {optionRoot} '*.net.*'        Select subchannels under 'net' across namespaces");
        Console.WriteLine($"  {optionRoot} '*.{{net,io}}'     Select 'net' and 'io' across all namespaces");
        Console.WriteLine($"  {optionRoot} '{{alpha,beta}}.*' Select all top-level channels in alpha and beta");
        Console.WriteLine($"  {optionRoot} alpha.net");
        Console.WriteLine($"  {optionRoot} beta.scheduler.tick");
        Console.WriteLine($"  {optionRoot} alpha.net,beta.io");
        Console.WriteLine($"  {optionRoot} gamma.physics.*");
        Console.WriteLine($"  {optionRoot} gamma.physics.*.*");
        Console.WriteLine($"  {optionRoot} alpha.{{net,cache}}");
        Console.WriteLine($"  {optionRoot} beta.{{io,scheduler}}.packet");
        Console.WriteLine($"  {optionRoot} '{{alpha,beta}}.net'");
        Console.WriteLine();
    }
}

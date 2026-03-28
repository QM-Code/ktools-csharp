using System;
using Kcli;

namespace Ktrace;

public sealed partial class Logger
{
    public InlineParser MakeInlineParser(TraceLogger localTraceLogger, string traceRoot = "trace")
    {
        string localNamespace = localTraceLogger?.Namespace ?? string.Empty;
        InlineParser parser = new InlineParser(string.IsNullOrWhiteSpace(traceRoot) ? "trace" : traceRoot);
        parser.SetRootValueHandler((_, value) => EnableChannels(value, localNamespace), "<channels>", "Trace selected channels.");
        parser.SetHandler("-examples", context => TraceCliRenderer.WriteExamples(context.Root), "Show selector examples.");
        parser.SetHandler("-namespaces", _ => TraceCliRenderer.WriteNamespaces(GetNamespaces()), "Show initialized trace namespaces.");
        parser.SetHandler("-channels", _ => TraceCliRenderer.WriteChannels(this), "Show initialized trace channels.");
        parser.SetHandler("-colors", _ => TraceCliRenderer.WriteColors(), "Show available trace colors.");
        parser.SetHandler("-files", _ =>
        {
            OutputOptions options = GetOutputOptions();
            options.Filenames = true;
            options.LineNumbers = true;
            SetOutputOptions(options);
        }, "Include source file and line in trace output.");
        parser.SetHandler("-functions", _ =>
        {
            OutputOptions options = GetOutputOptions();
            options.Filenames = true;
            options.LineNumbers = true;
            options.FunctionNames = true;
            SetOutputOptions(options);
        }, "Include function names in trace output.");
        parser.SetHandler("-timestamps", _ =>
        {
            OutputOptions options = GetOutputOptions();
            options.Timestamps = true;
            SetOutputOptions(options);
        }, "Include timestamps in trace output.");
        return parser;
    }
}

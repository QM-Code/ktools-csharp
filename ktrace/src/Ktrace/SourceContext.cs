using System.Diagnostics;

namespace Ktrace;

internal readonly struct SourceContext
{
    public SourceContext(string filePath, int lineNumber, string memberName)
    {
        FilePath = filePath ?? string.Empty;
        LineNumber = lineNumber;
        MemberName = memberName ?? string.Empty;
    }

    public string FilePath { get; }
    public int LineNumber { get; }
    public string MemberName { get; }

    public static SourceContext Capture(int skipFrames)
    {
        StackTrace trace = new StackTrace(skipFrames + 1, true);
        StackFrame frame = trace.GetFrame(0);
        if (frame == null)
        {
            return new SourceContext(string.Empty, 0, string.Empty);
        }

        string memberName = frame.GetMethod()?.Name ?? string.Empty;
        return new SourceContext(frame.GetFileName() ?? string.Empty, frame.GetFileLineNumber(), memberName);
    }
}

using Kcli;
using Kcli.Demo.Common;

namespace Kcli.Demo.Alpha;

public static class AlphaSdk
{
    public static InlineParser GetInlineParser()
    {
        InlineParser parser = new InlineParser("--alpha");
        parser.SetHandler("-message", DemoSupport.PrintProcessingLine, "Set alpha message label.");
        parser.SetOptionalValueHandler("-enable", DemoSupport.PrintProcessingLine, "Enable alpha processing.");
        return parser;
    }
}

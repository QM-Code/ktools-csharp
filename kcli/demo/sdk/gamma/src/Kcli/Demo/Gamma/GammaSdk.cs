using Kcli;
using Kcli.Demo.Common;

namespace Kcli.Demo.Gamma;

public static class GammaSdk
{
    public static InlineParser GetInlineParser()
    {
        InlineParser parser = new InlineParser("--gamma");
        parser.SetOptionalValueHandler("-strict", DemoSupport.PrintProcessingLine, "Enable strict gamma mode.");
        parser.SetHandler("-tag", DemoSupport.PrintProcessingLine, "Set a gamma tag label.");
        return parser;
    }
}

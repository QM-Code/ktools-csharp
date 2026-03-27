using System;
using Kcli;
using Kcli.Demo.Common;

namespace Kcli.Demo.Beta;

public static class BetaSdk
{
    public static InlineParser GetInlineParser()
    {
        InlineParser parser = new InlineParser("--beta");
        parser.SetHandler("-profile", DemoSupport.PrintProcessingLine, "Select beta runtime profile.");
        parser.SetHandler("-workers", (context, value) =>
        {
            if (!int.TryParse(value, out _))
            {
                throw new InvalidOperationException("expected an integer");
            }
            DemoSupport.PrintProcessingLine(context, value);
        }, "Set beta worker count.");
        return parser;
    }
}

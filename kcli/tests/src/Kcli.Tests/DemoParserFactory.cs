namespace Kcli.Tests;

internal static class DemoParserFactory
{
    public static Parser CreateBootstrapParser(FlagHandler onVerbose)
    {
        Parser parser = new Parser();
        parser.AddAlias("-v", "--verbose");
        parser.SetHandler("--verbose", onVerbose, "Enable verbose demo logging.");
        return parser;
    }

    public static Parser CreateCoreParser(FlagHandler onVerbose, ValueHandler onOutput, ValueHandler onAlphaEnable)
    {
        Parser parser = new Parser();
        InlineParser alpha = new InlineParser("--alpha");
        alpha.SetHandler("-message", (_, _) => { }, "Set alpha message label.");
        alpha.SetOptionalValueHandler("-enable", onAlphaEnable, "Enable alpha processing.");

        parser.AddInlineParser(alpha);
        parser.AddAlias("-v", "--verbose");
        parser.AddAlias("-out", "--output");
        parser.AddAlias("-a", "--alpha-enable");
        parser.SetHandler("--verbose", onVerbose, "Enable verbose app logging.");
        parser.SetHandler("--output", onOutput, "Set app output target.");
        return parser;
    }

    public static Parser CreateOmegaParser(
        FlagHandler onVerbose,
        ValueHandler onOutput,
        ValueHandler onAlphaEnable,
        ValueHandler onBetaWorkers,
        ValueHandler onGammaTag,
        ValueHandler onBuildProfile,
        PositionalHandler onPositionals)
    {
        Parser parser = new Parser();

        InlineParser alpha = new InlineParser("--alpha");
        alpha.SetHandler("-message", (_, _) => { }, "Set alpha message label.");
        alpha.SetOptionalValueHandler("-enable", onAlphaEnable, "Enable alpha processing.");

        InlineParser beta = new InlineParser("--beta");
        beta.SetHandler("-profile", (_, _) => { }, "Select beta runtime profile.");
        beta.SetHandler("-workers", onBetaWorkers, "Set beta worker count.");

        InlineParser gamma = new InlineParser("--gamma");
        gamma.SetOptionalValueHandler("-strict", (_, _) => { }, "Enable strict gamma mode.");
        gamma.SetHandler("-tag", onGammaTag, "Set a gamma tag label.");
        gamma.SetRoot("--newgamma");

        InlineParser build = new InlineParser("--build");
        build.SetHandler("-profile", onBuildProfile, "Set build profile.");
        build.SetHandler("-clean", _ => { }, "Enable clean build.");

        parser.AddInlineParser(alpha);
        parser.AddInlineParser(beta);
        parser.AddInlineParser(gamma);
        parser.AddInlineParser(build);

        parser.AddAlias("-v", "--verbose");
        parser.AddAlias("-out", "--output");
        parser.AddAlias("-a", "--alpha-enable");
        parser.AddAlias("-b", "--build-profile");

        parser.SetHandler("--verbose", onVerbose, "Enable verbose app logging.");
        parser.SetHandler("--output", onOutput, "Set app output target.");
        parser.SetPositionalHandler(onPositionals);
        return parser;
    }
}

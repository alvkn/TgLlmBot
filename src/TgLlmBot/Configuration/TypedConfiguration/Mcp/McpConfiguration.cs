using System;
using TgLlmBot.Configuration.Options.Mcp;

namespace TgLlmBot.Configuration.TypedConfiguration.Mcp;

public class McpConfiguration
{
    private McpConfiguration(
        McpGithubConfiguration github,
        McpBraveConfiguration brave,
        McpContext7Configuration context7,
        McpBrightDataConfiguration brightData)
    {
        ArgumentNullException.ThrowIfNull(github);
        ArgumentNullException.ThrowIfNull(brave);
        ArgumentNullException.ThrowIfNull(context7);
        ArgumentNullException.ThrowIfNull(brightData);
        Github = github;
        Brave = brave;
        Context7 = context7;
        BrightData = brightData;
    }

    public McpGithubConfiguration Github { get; }
    public McpBraveConfiguration Brave { get; }
    public McpContext7Configuration Context7 { get; }
    public McpBrightDataConfiguration BrightData { get; }

    public static McpConfiguration Convert(McpOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var github = McpGithubConfiguration.Convert(options.Github);
        var brave = McpBraveConfiguration.Convert(options.Brave);
        var context7 = McpContext7Configuration.Convert(options.Context7);
        var brightData = McpBrightDataConfiguration.Convert(options.BrightData);
        return new(github, brave, context7, brightData);
    }
}

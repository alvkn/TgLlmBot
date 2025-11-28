using System;
using TgLlmBot.Configuration.Options.Mcp;

namespace TgLlmBot.Configuration.TypedConfiguration.Mcp;

public class McpConfiguration
{
    private McpConfiguration(
        McpGithubConfiguration github,
        McpBraveConfiguration brave,
        McpContext7Configuration context7)
    {
        ArgumentNullException.ThrowIfNull(github);
        ArgumentNullException.ThrowIfNull(brave);
        ArgumentNullException.ThrowIfNull(context7);
        Github = github;
        Brave = brave;
        Context7 = context7;
    }

    public McpGithubConfiguration Github { get; }
    public McpBraveConfiguration Brave { get; }
    public McpContext7Configuration Context7 { get; }

    public static McpConfiguration Convert(McpOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var github = McpGithubConfiguration.Convert(options.Github);
        var brave = McpBraveConfiguration.Convert(options.Brave);
        var context7 = McpContext7Configuration.Convert(options.Context7);
        return new(github, brave, context7);
    }
}

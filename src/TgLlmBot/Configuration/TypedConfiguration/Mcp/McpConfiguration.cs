using System;
using TgLlmBot.Configuration.Options.Mcp;

namespace TgLlmBot.Configuration.TypedConfiguration.Mcp;

public class McpConfiguration
{
    private McpConfiguration(McpGithubConfiguration github, McpBraveConfiguration brave)
    {
        ArgumentNullException.ThrowIfNull(github);
        ArgumentNullException.ThrowIfNull(brave);
        Github = github;
        Brave = brave;
    }

    public McpGithubConfiguration Github { get; }
    public McpBraveConfiguration Brave { get; }

    public static McpConfiguration Convert(McpOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var github = McpGithubConfiguration.Convert(options.Github);
        var brave = McpBraveConfiguration.Convert(options.Brave);
        return new(github, brave);
    }
}

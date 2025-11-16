using System;
using TgLlmBot.Configuration.Options.Mcp;

namespace TgLlmBot.Configuration.TypedConfiguration.Mcp;

public class McpBraveConfiguration
{
    private McpBraveConfiguration(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(apiKey));
        }

        ApiKey = apiKey;
    }

    public string ApiKey { get; }

    public static McpBraveConfiguration Convert(McpBraveOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new(options.ApiKey);
    }
}

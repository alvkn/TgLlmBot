using System;
using TgLlmBot.Configuration.Options.Mcp;

namespace TgLlmBot.Configuration.TypedConfiguration.Mcp;

public class McpContext7Configuration
{
    private McpContext7Configuration(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(apiKey));
        }

        ApiKey = apiKey;
    }

    public string ApiKey { get; }

    public static McpContext7Configuration Convert(McpContext7Options options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new(options.ApiKey);
    }
}

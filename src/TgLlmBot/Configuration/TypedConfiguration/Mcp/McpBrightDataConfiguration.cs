using System;
using TgLlmBot.Configuration.Options.Mcp;

namespace TgLlmBot.Configuration.TypedConfiguration.Mcp;

public class McpBrightDataConfiguration
{
    private McpBrightDataConfiguration(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(apiKey));
        }

        ApiKey = apiKey;
    }

    public string ApiKey { get; }

    public static McpBrightDataConfiguration Convert(McpBrightDataOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new(options.ApiKey);
    }
}

using System;

namespace TgLlmBot.Services.Mcp.Clients.Context7;

public class DefaultContext7McpClientFactoryOptions
{
    public DefaultContext7McpClientFactoryOptions(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(apiKey));
        }

        ApiKey = apiKey;
    }

    public string ApiKey { get; }
}

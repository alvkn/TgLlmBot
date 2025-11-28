using System;

namespace TgLlmBot.Services.Mcp.Clients.BrightData;

public class DefaultBrightDataMcpClientFactoryOptions
{
    public DefaultBrightDataMcpClientFactoryOptions(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(apiKey));
        }

        ApiKey = apiKey;
    }

    public string ApiKey { get; }
}

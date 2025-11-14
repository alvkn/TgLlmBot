using System;

namespace TgLlmBot.Services.OpenRouter;

public class DefaultOpenRouterKeyUsageProviderOptions
{
    public DefaultOpenRouterKeyUsageProviderOptions(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(apiKey));
        }

        ApiKey = apiKey;
    }

    public string ApiKey { get; }
}

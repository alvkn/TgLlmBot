using System;

namespace TgLlmBot.CommandDispatcher;

public class DefaultTelegramCommandDispatcherOptions
{
    public DefaultTelegramCommandDispatcherOptions(string botName)
    {
        if (string.IsNullOrWhiteSpace(botName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(botName));
        }

        BotName = botName;
    }

    public string BotName { get; }
}

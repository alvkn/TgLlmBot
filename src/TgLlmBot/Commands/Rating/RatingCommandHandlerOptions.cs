using System;

namespace TgLlmBot.Commands.Rating;

public class RatingCommandHandlerOptions
{
    public RatingCommandHandlerOptions(string botName)
    {
        if (string.IsNullOrWhiteSpace(botName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(botName));
        }

        BotName = botName;
    }

    public string BotName { get; }
}

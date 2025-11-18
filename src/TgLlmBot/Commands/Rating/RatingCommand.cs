using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgLlmBot.CommandDispatcher.Abstractions;

namespace TgLlmBot.Commands.Rating;

public class RatingCommand : AbstractCommand
{
    public RatingCommand(Message message, UpdateType type) : base(message, type)
    {
    }
}

using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgLlmBot.CommandDispatcher.Abstractions;

namespace TgLlmBot.Commands.Usage;

public class UsageCommand : AbstractCommand
{
    public UsageCommand(Message message, UpdateType type) : base(message, type)
    {
    }
}

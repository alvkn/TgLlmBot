using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgLlmBot.CommandDispatcher.Abstractions;

namespace TgLlmBot.Commands.Ping;

public class PingCommand : AbstractCommand
{
    public PingCommand(Message message, UpdateType type) : base(message, type)
    {
    }
}

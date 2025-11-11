using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgLlmBot.Services.Telegram.CommandDispatcher.Abstractions;

namespace TgLlmBot.Commands.Ping;

public class PingCommand : AbstractCommand
{
    public PingCommand(Message message, UpdateType type) : base(message, type)
    {
    }
}

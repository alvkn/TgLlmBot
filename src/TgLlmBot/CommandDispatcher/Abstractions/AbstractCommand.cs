using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TgLlmBot.CommandDispatcher.Abstractions;

public abstract class AbstractCommand
{
    protected AbstractCommand(Message message, UpdateType type)
    {
        Message = message;
        Type = type;
    }

    public Message Message { get; }
    public UpdateType Type { get; }
}

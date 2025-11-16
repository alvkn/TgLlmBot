using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgLlmBot.CommandDispatcher.Abstractions;

namespace TgLlmBot.Commands.Model;

public class ModelCommand : AbstractCommand
{
    public ModelCommand(Message message, UpdateType type) : base(message, type)
    {
    }
}

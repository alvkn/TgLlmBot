using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgLlmBot.CommandDispatcher.Abstractions;

namespace TgLlmBot.Commands.DisplayHelp;

public class DisplayHelpCommand : AbstractCommand
{
    public DisplayHelpCommand(Message message, UpdateType type) : base(message, type)
    {
    }
}

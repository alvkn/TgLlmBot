using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgLlmBot.CommandDispatcher.Abstractions;

namespace TgLlmBot.Commands.SetPersonalSystemPrompt;

public class SetPersonalSystemPromptCommand : AbstractCommand
{
    public SetPersonalSystemPromptCommand(Message message, UpdateType type, User self) : base(message, type)
    {
        Self = self;
    }

    public User Self { get; }
}

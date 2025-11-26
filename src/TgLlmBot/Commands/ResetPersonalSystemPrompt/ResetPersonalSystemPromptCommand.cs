using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgLlmBot.CommandDispatcher.Abstractions;

namespace TgLlmBot.Commands.ResetPersonalSystemPrompt;

public class ResetPersonalSystemPromptCommand : AbstractCommand
{
    public ResetPersonalSystemPromptCommand(Message message, UpdateType type, User self) : base(message, type)
    {
        Self = self;
    }

    public User Self { get; }
}

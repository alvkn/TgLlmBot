using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgLlmBot.CommandDispatcher.Abstractions;

namespace TgLlmBot.Commands.ResetChatSystemPrompt;

public class ResetChatSystemPromptCommand : AbstractCommand
{
    public ResetChatSystemPromptCommand(Message message, UpdateType type, User self) : base(message, type)
    {
        Self = self;
    }

    public User Self { get; }
}

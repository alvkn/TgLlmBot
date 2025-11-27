using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgLlmBot.CommandDispatcher.Abstractions;

namespace TgLlmBot.Commands.ShowChatSystemPrompt;

public class ShowChatSystemPromptCommand : AbstractCommand
{
    public ShowChatSystemPromptCommand(Message message, UpdateType type, User self) : base(message, type)
    {
        Self = self;
    }

    public User Self { get; }
}

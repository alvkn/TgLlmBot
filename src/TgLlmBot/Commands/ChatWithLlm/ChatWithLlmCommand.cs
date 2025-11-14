using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgLlmBot.CommandDispatcher.Abstractions;

namespace TgLlmBot.Commands.ChatWithLlm;

public class ChatWithLlmCommand : AbstractCommand
{
    public ChatWithLlmCommand(Message message, UpdateType type, User self, string? prompt) : base(message, type)
    {
        Self = self;
        Prompt = prompt;
    }

    public User Self { get; }

    public string? Prompt { get; }
}

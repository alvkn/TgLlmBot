using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgLlmBot.Services.Telegram.CommandDispatcher.Abstractions;

namespace TgLlmBot.Commands.Repo;

public class RepoCommand : AbstractCommand
{
    public RepoCommand(Message message, UpdateType type) : base(message, type)
    {
    }
}

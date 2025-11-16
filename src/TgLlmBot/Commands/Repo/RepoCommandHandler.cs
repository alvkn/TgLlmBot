using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using TgLlmBot.CommandDispatcher.Abstractions;

namespace TgLlmBot.Commands.Repo;

public class RepoCommandHandler : AbstractCommandHandler<RepoCommand>
{
    private readonly TelegramBotClient _bot;

    public RepoCommandHandler(TelegramBotClient bot)
    {
        ArgumentNullException.ThrowIfNull(bot);
        _bot = bot;
    }

    public override async Task HandleAsync(RepoCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(command);
        await _bot.SendMessage(
            command.Message.Chat,
            "https://github.com/NetGreenChat/TgLlmBot",
            replyParameters: new()
            {
                MessageId = command.Message.MessageId
            },
            cancellationToken: cancellationToken);
    }
}

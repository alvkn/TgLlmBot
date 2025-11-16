using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using TgLlmBot.CommandDispatcher.Abstractions;

namespace TgLlmBot.Commands.Ping;

public class PingCommandHandler : AbstractCommandHandler<PingCommand>
{
    private readonly TelegramBotClient _bot;

    public PingCommandHandler(TelegramBotClient bot)
    {
        ArgumentNullException.ThrowIfNull(bot);
        _bot = bot;
    }

    public override async Task HandleAsync(PingCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(command);
        await _bot.SendMessage(
            command.Message.Chat,
            "Pong",
            replyParameters: new()
            {
                MessageId = command.Message.MessageId
            },
            cancellationToken: cancellationToken);
    }
}

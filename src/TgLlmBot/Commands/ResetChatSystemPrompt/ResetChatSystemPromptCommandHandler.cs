using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TgLlmBot.CommandDispatcher.Abstractions;
using TgLlmBot.Services.DataAccess.SystemPrompts;
using TgLlmBot.Services.DataAccess.TelegramMessages;

namespace TgLlmBot.Commands.ResetChatSystemPrompt;

public class ResetChatSystemPromptCommandHandler : AbstractCommandHandler<ResetChatSystemPromptCommand>
{
    private readonly TelegramBotClient _bot;
    private readonly ITelegramMessageStorage _storage;
    private readonly ISystemPromptService _systemPrompt;

    public ResetChatSystemPromptCommandHandler(TelegramBotClient bot, ISystemPromptService systemPrompt, ITelegramMessageStorage storage)
    {
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(systemPrompt);
        ArgumentNullException.ThrowIfNull(storage);
        _bot = bot;
        _systemPrompt = systemPrompt;
        _storage = storage;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public override async Task HandleAsync(ResetChatSystemPromptCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await _systemPrompt.ResetChatPromptAsync(command.Message.Chat.Id, cancellationToken);
            var response = await _bot.SendMessage(
                command.Message.Chat,
                "👌 Теперь для чата использую стандартный системный промпт",
                ParseMode.MarkdownV2,
                new()
                {
                    MessageId = command.Message.MessageId
                },
                cancellationToken: cancellationToken);
            await _storage.StoreMessageAsync(response, command.Self, cancellationToken);
        }
        catch (Exception ex)
        {
            var response = await _bot.SendMessage(
                command.Message.Chat,
                ex.Message,
                ParseMode.None,
                new()
                {
                    MessageId = command.Message.MessageId
                },
                cancellationToken: cancellationToken);
            await _storage.StoreMessageAsync(response, command.Self, cancellationToken);
        }
    }
}

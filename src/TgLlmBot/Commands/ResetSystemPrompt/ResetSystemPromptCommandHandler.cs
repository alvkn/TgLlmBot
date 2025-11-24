using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TgLlmBot.CommandDispatcher.Abstractions;
using TgLlmBot.Services.DataAccess;
using TgLlmBot.Services.Llm.Chat;

namespace TgLlmBot.Commands.ResetSystemPrompt;

public class ResetSystemPromptCommandHandler : AbstractCommandHandler<ResetSystemPromptCommand>
{
    private readonly TelegramBotClient _bot;
    private readonly ICustomChatSystemPromptService _chatSystemPrompt;
    private readonly ITelegramMessageStorage _storage;

    public ResetSystemPromptCommandHandler(TelegramBotClient bot, ICustomChatSystemPromptService chatSystemPrompt, ITelegramMessageStorage storage)
    {
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(chatSystemPrompt);
        ArgumentNullException.ThrowIfNull(storage);
        _bot = bot;
        _chatSystemPrompt = chatSystemPrompt;
        _storage = storage;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public override async Task HandleAsync(ResetSystemPromptCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            _chatSystemPrompt.Reset(command.Message.Chat.Id);
            var response = await _bot.SendMessage(
                command.Message.Chat,
                "👌 Теперь использую стандартный системный промпт",
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

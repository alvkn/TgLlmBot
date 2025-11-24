using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TgLlmBot.CommandDispatcher.Abstractions;
using TgLlmBot.Services.DataAccess;
using TgLlmBot.Services.Llm.Chat;

namespace TgLlmBot.Commands.SetSystemPrompt;

public class SetSystemPromptCommandHandler : AbstractCommandHandler<SetSystemPromptCommand>
{
    private readonly TelegramBotClient _bot;
    private readonly ICustomChatSystemPromptService _chatSystemPrompt;
    private readonly ITelegramMessageStorage _storage;

    public SetSystemPromptCommandHandler(
        TelegramBotClient bot,
        ICustomChatSystemPromptService chatSystemPrompt,
        ITelegramMessageStorage storage)
    {
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(chatSystemPrompt);
        ArgumentNullException.ThrowIfNull(storage);
        _bot = bot;
        _chatSystemPrompt = chatSystemPrompt;
        _storage = storage;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public override async Task HandleAsync(SetSystemPromptCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var prompt = $"{command.Message.Text?.Trim()}".Trim();
            if (prompt.StartsWith("!role", StringComparison.Ordinal))
            {
                prompt = prompt["!role".Length..].Trim();
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                var response = await _bot.SendMessage(
                    command.Message.Chat,
                    "❌ Не удалось поменять системный промпт",
                    ParseMode.MarkdownV2,
                    new()
                    {
                        MessageId = command.Message.MessageId
                    },
                    cancellationToken: cancellationToken);
                await _storage.StoreMessageAsync(response, command.Self, cancellationToken);
            }
            else
            {
                _chatSystemPrompt.Set(command.Message.Chat.Id, prompt);
                var response = await _bot.SendMessage(
                    command.Message.Chat,
                    "✅ Системный промпт успешно изменён",
                    ParseMode.MarkdownV2,
                    new()
                    {
                        MessageId = command.Message.MessageId
                    },
                    cancellationToken: cancellationToken);
                await _storage.StoreMessageAsync(response, command.Self, cancellationToken);
            }
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

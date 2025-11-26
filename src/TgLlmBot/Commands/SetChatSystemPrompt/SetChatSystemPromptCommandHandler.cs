using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TgLlmBot.CommandDispatcher.Abstractions;
using TgLlmBot.Services.DataAccess.SystemPrompts;
using TgLlmBot.Services.DataAccess.TelegramMessages;

namespace TgLlmBot.Commands.SetChatSystemPrompt;

public class SetChatSystemPromptCommandHandler : AbstractCommandHandler<SetChatSystemPromptCommand>
{
    private readonly TelegramBotClient _bot;
    private readonly ITelegramMessageStorage _storage;
    private readonly ISystemPromptService _systemPrompt;

    public SetChatSystemPromptCommandHandler(
        TelegramBotClient bot,
        ISystemPromptService systemPrompt,
        ITelegramMessageStorage storage)
    {
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(systemPrompt);
        ArgumentNullException.ThrowIfNull(storage);
        _bot = bot;
        _systemPrompt = systemPrompt;
        _storage = storage;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public override async Task HandleAsync(SetChatSystemPromptCommand command, CancellationToken cancellationToken)
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
                    "❌ Не удалось поменять системный промпт чата",
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
                await _systemPrompt.SetChatPromptAsync(command.Message.Chat.Id, prompt, cancellationToken);
                var response = await _bot.SendMessage(
                    command.Message.Chat,
                    "✅ Системный промпт чата успешно изменён",
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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgLlmBot.CommandDispatcher.Abstractions;
using TgLlmBot.DataAccess.Models;
using TgLlmBot.Services.DataAccess;
using TgLlmBot.Services.OpenAIClient.Costs;
using TgLlmBot.Services.Telegram.Markdown;
using TgLlmBot.Services.Telegram.TypingStatus;

namespace TgLlmBot.Commands.Rating;

public class RatingCommandHandler : AbstractCommandHandler<RatingCommand>
{
    private const string LlmResponseJsonSchema = """
                                                 {
                                                     "$schema": "https://json-schema.org/draft/2020-12/schema",
                                                     "type": "object",
                                                     "properties": {
                                                         "Data": {
                                                             "type": "array",
                                                             "items": {
                                                                 "type": "object",
                                                                 "properties": {
                                                                     "FromUserId": {
                                                                         "type": "integer",
                                                                         "format": "int64"
                                                                     },
                                                                     "Level": {
                                                                         "type": "integer",
                                                                         "minimum": 0,
                                                                         "maximum": 100
                                                                     }
                                                                 },
                                                                 "required": [
                                                                     "FromUserId",
                                                                     "Level"
                                                                 ],
                                                                 "additionalProperties": false
                                                             }
                                                         }
                                                     },
                                                     "required": [
                                                         "Data"
                                                     ],
                                                     "additionalProperties": false
                                                 }
                                                 """;

    private static readonly CultureInfo RuCulture = new("ru-RU");

    private static readonly JsonSerializerOptions HistorySerializationOptions = new(JsonSerializerDefaults.General)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = false
    };

    private readonly TelegramBotClient _bot;
    private readonly IChatClient _chatClient;
    private readonly ICostContextStorage _costContextStorage;

    private readonly RatingCommandHandlerOptions _options;
    private readonly ITelegramMessageStorage _storage;
    private readonly ITelegramMarkdownConverter _telegramMarkdownConverter;
    private readonly TimeProvider _timeProvider;
    private readonly ITypingStatusService _typingStatusService;

    public RatingCommandHandler(
        RatingCommandHandlerOptions options,
        TelegramBotClient bot,
        IChatClient chatClient,
        ICostContextStorage costContextStorage,
        ITelegramMessageStorage storage,
        ITelegramMarkdownConverter telegramMarkdownConverter,
        TimeProvider timeProvider,
        ITypingStatusService typingStatusService)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(chatClient);
        ArgumentNullException.ThrowIfNull(costContextStorage);
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(telegramMarkdownConverter);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(typingStatusService);
        _options = options;
        _bot = bot;
        _chatClient = chatClient;
        _costContextStorage = costContextStorage;
        _storage = storage;
        _telegramMarkdownConverter = telegramMarkdownConverter;
        _timeProvider = timeProvider;
        _typingStatusService = typingStatusService;
    }


    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public override async Task HandleAsync(RatingCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            _costContextStorage.Initialize();
            _typingStatusService.StartTyping(command.Message.Chat.Id);
            var contextMessages = await _storage.SelectContextMessagesAsync(
                command.Message,
                cancellationToken);
            var context = BuildContext(command.Self, contextMessages);
            var responseFormat = ChatResponseFormat.ForJsonSchema(
                JsonSerializer.Deserialize<JsonDocument>(LlmResponseJsonSchema, AIJsonUtilities.DefaultOptions)!.RootElement,
                "chat_cringe_rating_data",
                "Ответ на запрос об анализе уровня кринжа в чате");
            var chatOptions = new ChatOptions
            {
                ConversationId = Guid.NewGuid().ToString("N"),
                Temperature = 0.3f,
                AllowMultipleToolCalls = false,
                ToolMode = new NoneChatToolMode(),
                ResponseFormat = responseFormat
            };
            var llmResponse = await _chatClient.GetResponseAsync(context, chatOptions, cancellationToken);
            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            var rawLLmResponse = llmResponse.Text?.Trim();
            _typingStatusService.StopTyping(command.Message.Chat.Id);

            if (TryDeserializeLlmResponse(rawLLmResponse, out var data))
            {
                var stats = GetShitposterStats(command, contextMessages, data);
                if (stats.Length > 0)
                {
                    var report = BuildRatingReport(stats, contextMessages);
                    await RespondWithMarkdownAsync(command, report, cancellationToken);
                }
                else
                {
                    await RespondWithMarkdownAsync(command, "🤷 LLM ответила корректно, но не хватило данных", cancellationToken);
                }
            }
            else
            {
                await RespondWithMarkdownAsync(command, "😞 Не удалось проанализировать сообщения", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _typingStatusService.StopTyping(command.Message.Chat.Id);
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

    private async Task RespondWithMarkdownAsync(RatingCommand command, string errorText, CancellationToken cancellationToken)
    {
        var costInUsd = 0m;
        if (_costContextStorage.TryGetCost(out var cost))
        {
            costInUsd = cost.Value;
        }

        var costTextPresent = false;
        var costText = $"[Cost: {costInUsd} USD]";
        var responseText = errorText;
        if (costInUsd > 0m)
        {
            responseText += $"\n\n{costText}";
            costTextPresent = true;
        }

        var telegramMarkdown = _telegramMarkdownConverter.ConvertToTelegramMarkdown(responseText);
        var response = await _bot.SendMessage(
            command.Message.Chat,
            telegramMarkdown,
            ParseMode.MarkdownV2,
            new()
            {
                MessageId = command.Message.MessageId
            },
            cancellationToken: cancellationToken);
        if (!string.IsNullOrEmpty(response.Text))
        {
            if (costTextPresent)
            {
                response.Text = response.Text[..^costText.Length].Trim();
            }
        }

        await _storage.StoreMessageAsync(response, command.Self, cancellationToken);
    }

    private static string BuildRatingReport(ShitposterStats[] stats, DbChatMessage[] contextMessages)
    {
        var builder = new StringBuilder();
        builder.AppendLine("🤡 **Рейтинг Щитпостеров** 🤡");
        builder.AppendLine();
        var top5 = stats.OrderByDescending(x => x.Level)
            .Take(5)
            .ToList();
        for (var i = 0; i < top5.Count; i++)
        {
            var userData = top5[i];
            var rank = i + 1;
            var medal = rank switch
            {
                1 => "🥇",
                2 => "🥈",
                3 => "🥉",
                _ => "  "
            };
            builder.AppendLine(CultureInfo.InvariantCulture, $"{medal} #{rank}: `{userData.Name}`");
            builder.AppendLine(CultureInfo.InvariantCulture, $"   Степень кринжа: {userData.Level:D}/100");
            builder.AppendLine();
            builder.Append("Проанализировано: ");
            builder.Append(contextMessages.Length);
            builder.AppendLine(" сообщений");
        }

        return builder.ToString();
    }

    private static ShitposterStats[] GetShitposterStats(
        RatingCommand command,
        DbChatMessage[] contextMessages,
        Dictionary<long, long> ratingsByUserId)
    {
        var selfUserId = command.Self.Id;
        var stats = new Dictionary<long, ShitposterStats>();
        var orderedMessages = contextMessages.OrderByDescending(x => x.Date).ToArray();
        foreach (var (userId, level) in ratingsByUserId)
        {
            if (userId == selfUserId)
            {
                continue;
            }

            var lastestUserMessage = orderedMessages.FirstOrDefault(x => x.FromUserId == userId);
            if (lastestUserMessage is not null)
            {
                var name = lastestUserMessage.FromUsername;
                if (string.IsNullOrWhiteSpace(name))
                {
                    var combinedName = $"{lastestUserMessage.FromFirstName?.Trim()} {lastestUserMessage.FromLastName?.Trim()}".Trim();
                    name = !string.IsNullOrWhiteSpace(combinedName)
                        ? combinedName
                        : "Anonymous";
                }

                if (!stats.ContainsKey(userId))
                {
                    stats.Add(userId, new(name, level));
                }
            }
        }

        return stats.Values.ToArray();
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private static bool TryDeserializeLlmResponse(string? rawResponse, [NotNullWhen(true)] out Dictionary<long, long>? data)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            data = null;
            return false;
        }

        try
        {
            var deserializedData = JsonSerializer.Deserialize<CringeRatingData?>(rawResponse, AIJsonUtilities.DefaultOptions);
            if (deserializedData?.Data is not null)
            {
                var resultAccumulator = new Dictionary<long, long>();
                foreach (var rating in deserializedData.Data)
                {
                    if (rating.FromUserId is not null
                        && rating.Level is >= 0 and <= 100)
                    {
                        resultAccumulator.Add(rating.FromUserId.Value, rating.Level.Value);
                    }
                }

                if (resultAccumulator.Count > 0)
                {
                    data = resultAccumulator;
                    return true;
                }
            }

            data = null;
            return false;
        }
        catch (Exception)
        {
            data = null;
            return false;
        }
    }

    private ChatMessage[] BuildContext(
        User self,
        DbChatMessage[] contextMessages)
    {
        var llmContext = new List<ChatMessage>
        {
            BuildSystemPrompt(self)
        };
        var historyContext = BuildHistoryContext(contextMessages);
        if (historyContext.Length > 0)
        {
            foreach (var chatMessage in historyContext)
            {
                llmContext.Add(chatMessage);
            }
        }

        var execPrompt = BuildExecutionPrompt();
        llmContext.Add(execPrompt);
        return llmContext.ToArray();
    }

    private ChatMessage BuildSystemPrompt(User self)
    {
        var roundUtcDate = DateTimeOffset.FromUnixTimeSeconds(_timeProvider.GetUtcNow().ToUnixTimeSeconds());
        var formattedDate = roundUtcDate.ToString("O", RuCulture);
        return new(
            ChatRole.System,
            $"""
             Ты - инструмент для анализа "уровня щитпостинга" в телеграм-чате, где есть бот, которого зовут "{_options.BotName}". Его {nameof(JsonHistoryMessage.FromUserId)}={self.Id}.

             Текущая дата и время по UTC: `{formattedDate}`
             """);
    }

    private static ChatMessage[] BuildHistoryContext(DbChatMessage[] contextMessages)
    {
        var history = contextMessages
            .Select(x => new JsonHistoryMessage(
                new DateTimeOffset(x.Date.Ticks, TimeSpan.Zero).ToUniversalTime(),
                x.MessageId,
                x.MessageThreadId,
                x.ReplyToMessageId,
                x.FromUserId,
                x.FromUsername?.Trim(),
                x.FromFirstName?.Trim(),
                x.FromLastName?.Trim(),
                (x.Text ?? x.Caption)?.Trim(),
                x.IsLlmReplyToMessage))
            .ToArray();
        var result = new List<ChatMessage>
        {
            new(ChatRole.User, $"""
                                Сейчас я тебе пришлю историю чата в формате JSON, где
                                {nameof(JsonHistoryMessage.DateTimeUtc)} - дата сообщения в UTC,
                                {nameof(JsonHistoryMessage.MessageId)} - Id сообщения
                                {nameof(JsonHistoryMessage.MessageThreadId)} - Id сообщения, с которого начался тред с цепочкой реплаев
                                {nameof(JsonHistoryMessage.ReplyToMessageId)} - Id оригинального сообщения, на которое даётся ответ (реплай)
                                {nameof(JsonHistoryMessage.FromUserId)} - Id автора сообщения
                                {nameof(JsonHistoryMessage.FromUsername)} - Username автора сообщения
                                {nameof(JsonHistoryMessage.FromFirstName)} - Имя автора сообщения
                                {nameof(JsonHistoryMessage.FromLastName)} - Фамилия автора сообщения
                                {nameof(JsonHistoryMessage.Text)} - текст сообщения
                                {nameof(JsonHistoryMessage.IsLlmReplyToMessage)} - флаг, обозначающий то что это ТЫ и отправил это сообщение в ответ кому-то
                                """)
        };
        foreach (var chatHistoryMessage in history)
        {
            var json = JsonSerializer.Serialize(chatHistoryMessage, HistorySerializationOptions);
            result.Add(new(ChatRole.User, json));
        }

        return result.ToArray();
    }

    private static ChatMessage BuildExecutionPrompt()
    {
        return new(ChatRole.User, $"""
                                   Проанализируй все эти сообщения и дай каждому пользователю оценку "уровень кринжа" по шкале от 0 до 100.

                                   Признаки кринжа:
                                   - Бессмысленные или провокационные реплики
                                   - Спам похожими сообщениями
                                   - Спам эмодзи, капслок
                                   - Низкокачественный или тупой юмор, "угар", мемы (при этом качественный/остроумный юмор - это НЕ кринж)

                                   При анализе истории сообщений - учитывай контекст обсуждений в которых он участвовал каждый пользователь (по связке {nameof(JsonHistoryMessage.FromUserId)} + {nameof(JsonHistoryMessage.MessageId)} + {nameof(JsonHistoryMessage.ReplyToMessageId)} или по связке {nameof(JsonHistoryMessage.FromUserId)} + {nameof(JsonHistoryMessage.MessageId)} + {nameof(JsonHistoryMessage.ReplyToMessageId)} + {nameof(JsonHistoryMessage.MessageThreadId)})

                                   Дай ответ в виде JSON, который будет содержать массив объектов.
                                   Каждый объект в массиве содержит оценку "уровня кринжа" отдельно-взятого пользователя.
                                   Каждый объект содержит свойства:
                                   - {nameof(CringeRating.FromUserId)} - Id автора сообщений (int64, целое число)
                                   - {nameof(CringeRating.Level)} - "уровень кринжа" сообщений этого пользователя (целое число в диапазоне от 0 (включительно) до 100 (включительно), где 0 - это серьёзное, качественное обсуждение, 50 - смесь серьёзного обсуждения и шуток, 100 - чистый щитпостинг (shitpost) / мемы (meme))
                                   """);
    }

    private sealed class CringeRatingData
    {
        public CringeRatingData(CringeRating[]? data)
        {
            Data = data;
        }

        public CringeRating[]? Data { get; }
    }

    private sealed class CringeRating
    {
        public CringeRating(long? fromUserId, long? level)
        {
            FromUserId = fromUserId;
            Level = level;
        }

        public long? FromUserId { get; }
        public long? Level { get; }
    }

    private sealed class JsonHistoryMessage
    {
        public JsonHistoryMessage(DateTimeOffset dateTimeUtc, int messageId, int? messageThreadId, int? replyToMessageId, long? fromUserId, string? fromUsername, string? fromFirstName, string? fromLastName, string? text, bool isLlmReplyToMessage)
        {
            DateTimeUtc = dateTimeUtc;
            MessageId = messageId;
            MessageThreadId = messageThreadId;
            ReplyToMessageId = replyToMessageId;
            FromUserId = fromUserId;
            FromUsername = fromUsername;
            FromFirstName = fromFirstName;
            FromLastName = fromLastName;
            Text = text;
            IsLlmReplyToMessage = isLlmReplyToMessage;
        }

        public DateTimeOffset DateTimeUtc { get; }
        public int MessageId { get; }
        public int? MessageThreadId { get; }
        public int? ReplyToMessageId { get; }
        public long? FromUserId { get; }
        public string? FromUsername { get; }
        public string? FromFirstName { get; }
        public string? FromLastName { get; }
        public string? Text { get; }
        public bool IsLlmReplyToMessage { get; }
    }

    private sealed class ShitposterStats
    {
        public ShitposterStats(string name, long level)
        {
            Name = name;
            Level = level;
        }

        public string Name { get; }
        public long Level { get; }
    }
}

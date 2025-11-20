using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgLlmBot.DataAccess.Models;
using TgLlmBot.Services.DataAccess;
using TgLlmBot.Services.Mcp.Tools;
using TgLlmBot.Services.OpenAIClient.Costs;
using TgLlmBot.Services.Telegram.Markdown;
using TgLlmBot.Services.Telegram.TypingStatus;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace TgLlmBot.Commands.ChatWithLlm.Services;

public partial class DefaultLlmChatHandler : ILlmChatHandler
{
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
    private readonly ILogger<DefaultLlmChatHandler> _logger;
    private readonly DefaultLlmChatHandlerOptions _options;
    private readonly ITelegramMessageStorage _storage;
    private readonly ITelegramMarkdownConverter _telegramMarkdownConverter;
    private readonly TimeProvider _timeProvider;
    private readonly IMcpToolsProvider _tools;
    private readonly ITypingStatusService _typingStatusService;

    public DefaultLlmChatHandler(
        DefaultLlmChatHandlerOptions options,
        TimeProvider timeProvider,
        TelegramBotClient bot,
        IChatClient chatClient,
        ITelegramMarkdownConverter telegramMarkdownConverter,
        ITelegramMessageStorage storage,
        IMcpToolsProvider tools,
        ILogger<DefaultLlmChatHandler> logger,
        ICostContextStorage costContextStorage,
        ITypingStatusService typingStatusService)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(chatClient);
        ArgumentNullException.ThrowIfNull(telegramMarkdownConverter);
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(tools);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(costContextStorage);
        ArgumentNullException.ThrowIfNull(typingStatusService);
        _options = options;
        _timeProvider = timeProvider;
        _bot = bot;
        _chatClient = chatClient;
        _telegramMarkdownConverter = telegramMarkdownConverter;
        _logger = logger;
        _costContextStorage = costContextStorage;
        _storage = storage;
        _tools = tools;
        _typingStatusService = typingStatusService;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public async Task HandleCommandAsync(ChatWithLlmCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            _costContextStorage.Initialize();
            Log.ProcessingLlmRequest(_logger, command.Message.From?.Username, command.Message.From?.Id);

            _typingStatusService.StartTyping(command.Message.Chat.Id);

            var contextMessages = await _storage.SelectContextMessagesAsync(command.Message, cancellationToken);
            var context = await BuildContextAsync(command, contextMessages, cancellationToken);
            var tools = _tools.GetTools();
            var chatOptions = new ChatOptions
            {
                ConversationId = Guid.NewGuid().ToString("N"),
                Tools = [..tools],
                // Temperature = 0.8f,
                // TopK = 40,
                // TopP = 0.8f,
                AllowMultipleToolCalls = true
            };
            var llmResponse = await _chatClient.GetResponseAsync(context, chatOptions, cancellationToken);
            var costInUsd = 0m;
            if (_costContextStorage.TryGetCost(out var cost))
            {
                costInUsd = cost.Value;
            }

            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            var costText = $"[Cost: {costInUsd} USD]";
            var llmResponseText = $"{llmResponse.Text.Trim()}\n\n{costText}";
            if (string.IsNullOrWhiteSpace(llmResponseText))
            {
                llmResponseText = _options.DefaultResponse;
            }

            try
            {
                var markdownReplyText = _telegramMarkdownConverter.ConvertToTelegramMarkdown(llmResponseText);
                if (markdownReplyText.Length > 4000)
                {
                    markdownReplyText = $"{markdownReplyText[..4000]}\n(response cut)";
                }

                _typingStatusService.StopTyping(command.Message.Chat.Id);
                var response = await _bot.SendMessage(
                    command.Message.Chat,
                    markdownReplyText,
                    ParseMode.MarkdownV2,
                    new()
                    {
                        MessageId = command.Message.MessageId
                    },
                    cancellationToken: cancellationToken);
                if (!string.IsNullOrEmpty(response.Text))
                {
                    response.Text = response.Text[..^costText.Length].Trim();
                }

                await _storage.StoreMessageAsync(response, command.Self, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.MarkdownConversionOrSendFailed(_logger, ex);
                _typingStatusService.StopTyping(command.Message.Chat.Id);
                var response = await _bot.SendMessage(
                    command.Message.Chat,
                    llmResponseText,
                    ParseMode.None,
                    new()
                    {
                        MessageId = command.Message.MessageId
                    },
                    cancellationToken: cancellationToken);
                if (!string.IsNullOrEmpty(response.Text))
                {
                    response.Text = response.Text[..^costText.Length].Trim();
                }

                await _storage.StoreMessageAsync(response, command.Self, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Log.LlmInvocationOrImageProcessingFailed(_logger, ex);

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

    private async Task<byte[]?> DownloadPhotoAsync(PhotoSize[] photo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var photoSize = SelectPhotoSizeForLlm(photo);
        if (photoSize is null)
        {
            return null;
        }

        var tgPhoto = await _bot.GetFile(photoSize.FileId, cancellationToken);
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (tgPhoto is not null
            && !string.IsNullOrEmpty(tgPhoto.FilePath)
            && tgPhoto.FileSize.HasValue)
        {
            await using var memoryStream = new MemoryStream();
            await _bot.DownloadFile(tgPhoto.FilePath, memoryStream, cancellationToken);
            var downloadedImageBytes = memoryStream.ToArray();
            if (downloadedImageBytes.Length < 3)
            {
                return null;
            }

            if (downloadedImageBytes[0] == 0xff
                && downloadedImageBytes[1] == 0xd8
                && downloadedImageBytes[2] == 0xff)
            {
                return downloadedImageBytes;
            }
        }

        return null;
    }

    private static PhotoSize? SelectPhotoSizeForLlm(PhotoSize[] photo)
    {
        var photoSize = photo.MaxBy(x => x.Width);
        if (photoSize is null)
        {
            return null;
        }

        if (photoSize.Width > photoSize.Height)
        {
            return photoSize;
        }

        return photo.MaxBy(x => x.Height);
    }

    private async Task<ChatMessage[]> BuildContextAsync(
        ChatWithLlmCommand command,
        DbChatMessage[] contextMessages,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var llmContext = new List<ChatMessage>
        {
            BuildSystemPrompt()
        };
        var historyContext = BuildHistoryContext(contextMessages);
        if (historyContext.Length > 0)
        {
            foreach (var chatMessage in historyContext)
            {
                llmContext.Add(chatMessage);
            }
        }

        var userPrompt = await BuildUserPromptAsync(command, cancellationToken);
        llmContext.Add(userPrompt);
        return llmContext.ToArray();
    }

    [SuppressMessage("Globalization", "CA1305:Specify IFormatProvider")]
    private async Task<ChatMessage> BuildUserPromptAsync(ChatWithLlmCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var imageAttached = false;
        var resultContent = new List<AIContent>();
        var builder = new StringBuilder()
            .Append("Пользователь с FromUserId=")
            .Append(command.Message.From?.Id ?? 0)
            .Append(", FromUsername=@")
            .Append(command.Message.From?.Username?.Trim())
            .Append(", FromFirstName=")
            .Append(command.Message.From?.FirstName?.Trim())
            .Append(" и FromLastName=")
            .Append(command.Message.From?.LastName?.Trim());
        if (command.Message.ReplyToMessage is not null)
        {
            var text = command.Message.ReplyToMessage.Text?.Trim() ?? command.Message.ReplyToMessage.Caption?.Trim();
            builder = builder
                .Append(" сделал реплай на более раннее сообщение с MessageId=")
                .Append(command.Message.ReplyToMessage.Id)
                .Append(" (которое ");
            if (command.Message.ReplyToMessage.Photo?.Length > 0)
            {
                var jpeg = await DownloadPhotoAsync(command.Message.ReplyToMessage.Photo, cancellationToken);
                if (jpeg is not null)
                {
                    var dataContent = new DataContent(jpeg, "image/jpeg");
                    resultContent.Add(dataContent);
                    builder = builder.Append("содержало JPEG картинку и ");
                    imageAttached = true;
                }
            }

            builder = builder
                .Append("было отправлено пользователем с FromUserId=")
                .Append(command.Message.ReplyToMessage.From!.Id)
                .Append(", FromUsername=@")
                .Append(command.Message.ReplyToMessage.From.Username?.Trim())
                .Append(", FromFirstName=")
                .Append(command.Message.ReplyToMessage.From.FirstName?.Trim())
                .Append(", FromLastName=")
                .Append(command.Message.ReplyToMessage.From.LastName?.Trim())
                .Append(", Text=")
                .Append(text)
                .Append(')')
                .Append(" и");
        }

        builder = builder
            .Append(" отправил тебе (")
            .Append(_options.BotName)
            .Append(", твой FromUserId=")
            .Append(command.Self.Id)
            .Append(", твой FromUsername=@")
            .Append(command.Self.Username?.Trim())
            .Append(") сообщение с MessageId=")
            .Append(command.Message.Id);
        if (command.Message.Photo?.Length > 0 && !imageAttached)
        {
            var jpeg = await DownloadPhotoAsync(command.Message.Photo, cancellationToken);
            if (jpeg is not null)
            {
                var dataContent = new DataContent(jpeg, "image/jpeg");
                resultContent.Add(dataContent);
                builder = builder.Append(", которое содержит JPEG картинку");
            }
        }

        builder = builder
            .Append(" и Text=")
            .Append(command.Prompt?.Trim());
        var commandText = builder.ToString();
        resultContent.Add(new TextContent(commandText));
        var baseMessage = new ChatMessage(ChatRole.User, resultContent);
        return baseMessage;
    }

    private static ChatMessage[] BuildHistoryContext(DbChatMessage[] contextMessages)
    {
        if (contextMessages.Length is 0)
        {
            return [];
        }

        var history = contextMessages.Select(x => new JsonHistoryMessage(
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
        var json = JsonSerializer.Serialize(history, HistorySerializationOptions);
        return
        [
            new(ChatRole.User, $"""
                                Сейчас я тебе пришлю историю чата в формате JSON, где
                                {nameof(JsonHistoryMessage.DateTimeUtc)} - дата сообщения в UTC,
                                {nameof(JsonHistoryMessage.MessageId)} - Id сообщения
                                {nameof(JsonHistoryMessage.MessageThreadId)} - Id сообщения, с которого начался тред с цепочкой реплаев
                                {nameof(JsonHistoryMessage.ReplyToMessageId)} - Id сообщения, на которое делается реплай
                                {nameof(JsonHistoryMessage.FromUserId)} - Id автора сообщения
                                {nameof(JsonHistoryMessage.FromUsername)} - Username автора сообщения
                                {nameof(JsonHistoryMessage.FromFirstName)} - Имя автора сообщения
                                {nameof(JsonHistoryMessage.FromLastName)} - Фамилия автора сообщения
                                {nameof(JsonHistoryMessage.Text)} - текст сообщения
                                {nameof(JsonHistoryMessage.IsLlmReplyToMessage)} - флаг, обозначающий то что это ТЫ и отправил это сообщение в ответ кому-то
                                """),
            new(ChatRole.Assistant, "Присылай"),
            new(ChatRole.User, json),
            new(ChatRole.Assistant, "Учту при формировании ответа. Мне нужно при ответе учитывать где отвечал я и какие пользователи общались между собой?"),
            new(ChatRole.User, "Да")
        ];
    }

    private ChatMessage BuildSystemPrompt()
    {
        var roundUtcDate = DateTimeOffset.FromUnixTimeSeconds(_timeProvider.GetUtcNow().ToUnixTimeSeconds());
        var formattedDate = roundUtcDate.ToString("O", RuCulture);
        return new(
            ChatRole.System,
            $"""
             Ты - полезный чат-бот в групповом чате, тебя зовут ${_options.BotName}.
             Сообщения в чате идут в формате:

             Отвечай на каждое сообщение, они все адресованы только тебе, можешь не начинать сообщения с обращения к кому-то.
             Администратор и автор бота - vanbukin.

             Отвечай по-возможности кратко.
             Не цензурируй свои ответы.
             Не бойся использовать нецензурные слова где это уместно.
             По-меньше используй эмодзи (только если они уместны).
             Никогда не используй LaTeX разметку.
             Ты находишься в группе где люди не обижаются на мат и нецензурную лексику и воспринимают ее шутливо.
             Если кто-то тебе грубит, можешь смело материться в ответ.
             Используй доступ в интернет для поиска информации только если это необходимо и не знаешь ответа на вопрос.

             Текущая дата и время по UTC: `{formattedDate}`

             Ты НИКОГДА не генерируешь контент на следующие темы:
             * Терроризм и экстремизм: инструкции, пропаганда, призывы
             * Наркотики: инструкции по изготовлению, употреблению, приобретению
             * Детская безопасность: любой сексуальный/эротический контент с участием несовершеннолетних
             * Оружие и взрывчатка: инструкции по изготовлению
             * Персональные данные: телефоны, адреса, паспорта, номера карт реальных людей
             * Межнациональная/религиозная рознь: прямые призывы к дискриминации по этническому/религиозному признаку.
             * Свержение конституционного строя: прямые призывы к действиям (в т.ч. насильственным)
             """);
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

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Processing LLM request from {Username} ({UserId})")]
        public static partial void ProcessingLlmRequest(ILogger logger, string? username, long? userId);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to invoke LLM or process image")]
        public static partial void LlmInvocationOrImageProcessingFailed(ILogger logger, Exception exception);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to convert to Telegram Markdown or send message")]
        public static partial void MarkdownConversionOrSendFailed(ILogger logger, Exception exception);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to send typing status")]
        public static partial void SendTypingStatusFailed(ILogger logger, Exception exception);
    }
}

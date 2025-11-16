using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgLlmBot.CommandDispatcher;

namespace TgLlmBot.Services.Telegram.RequestHandler;

public sealed partial class DefaultTelegramRequestHandler : ITelegramRequestHandler
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly TelegramBotClient _bot;
    private readonly ITelegramCommandDispatcher _commandDispatcher;
    private readonly ILogger<DefaultTelegramRequestHandler> _logger;
    private readonly DefaultTelegramRequestHandlerOptions _options;

    public DefaultTelegramRequestHandler(
        DefaultTelegramRequestHandlerOptions options,
        ITelegramCommandDispatcher commandDispatcher,
        IHostApplicationLifetime applicationLifetime,
        ILogger<DefaultTelegramRequestHandler> logger,
        TelegramBotClient bot)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(commandDispatcher);
        ArgumentNullException.ThrowIfNull(applicationLifetime);
        ArgumentNullException.ThrowIfNull(logger);
        _options = options;
        _commandDispatcher = commandDispatcher;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
        _bot = bot;
    }

    public async Task OnMessageAsync(Message message, UpdateType type)
    {
        ArgumentNullException.ThrowIfNull(message);
        await OnMessageInternalAsync(message, type, _applicationLifetime.ApplicationStopping);
    }

    public async Task OnErrorAsync(Exception exception, HandleErrorSource source)
    {
        await OnErrorInternalAsync(exception, source, _applicationLifetime.ApplicationStopping);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private async Task OnMessageInternalAsync(Message message, UpdateType type, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            if (message.Date < _options.SkipMessagesOlderThan)
            {
                return;
            }

            if (!_options.AllowedChatIds.Contains(message.Chat.Id))
            {
                return;
            }

            await _commandDispatcher.HandleMessageAsync(message, type, cancellationToken);
        }
        catch (Exception ex)
        {
            LogMessageHandlingException(_logger, ex);
        }
    }

    private Task OnErrorInternalAsync(Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        LogErrorHandling(_logger, source, exception);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Telegram error handling. Error source: {ErrorSource}")]
    private static partial void LogErrorHandling(ILogger logger, HandleErrorSource errorSource, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Got exception during message handling")]
    private static partial void LogMessageHandlingException(ILogger logger, Exception exception);
}

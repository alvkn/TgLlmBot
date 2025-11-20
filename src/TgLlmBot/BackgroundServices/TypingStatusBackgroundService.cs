using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TgLlmBot.Services.Telegram.TypingStatus;

namespace TgLlmBot.BackgroundServices;

public partial class TypingStatusBackgroundService : BackgroundService
{
    private const int TypingIntervalMs = 4_000;

    private readonly ConcurrentDictionary<long, CancellationTokenSource> _activeTypingTimersCts = new();
    private readonly TelegramBotClient _bot;
    private readonly ILogger<TypingStatusBackgroundService> _logger;

    private readonly ChannelReader<StartTypingCommand> _startTypingChannelReader;
    private readonly ChannelReader<StopTypingCommand> _stopTypingChannelReader;

    public TypingStatusBackgroundService(
        ChannelReader<StartTypingCommand> startTypingChannelReader,
        ChannelReader<StopTypingCommand> stopTypingChannelReader,
        TelegramBotClient bot,
        ILogger<TypingStatusBackgroundService> logger)
    {
        ArgumentNullException.ThrowIfNull(startTypingChannelReader);
        ArgumentNullException.ThrowIfNull(stopTypingChannelReader);
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(logger);
        _startTypingChannelReader = startTypingChannelReader;
        _stopTypingChannelReader = stopTypingChannelReader;
        _bot = bot;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogTypingStatusWorkerStarted();

        var startLoop = HandleStartTypingCommands(stoppingToken);
        var stopLoop = HandleStopTypingCommands(stoppingToken);

        await Task.WhenAll(startLoop, stopLoop);

        LogTypingStatusWorkerStopped();
    }

    private async Task HandleStartTypingCommands(CancellationToken stoppingToken)
    {
        await foreach (var cmd in _startTypingChannelReader.ReadAllAsync(stoppingToken))
        {
            if (_activeTypingTimersCts.ContainsKey(cmd.ChatId))
            {
                continue;
            }

            var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            if (_activeTypingTimersCts.TryAdd(cmd.ChatId, cts))
            {
                _ = RunTypingAsync(cmd.ChatId, cts.Token);
            }
        }
    }

    private async Task HandleStopTypingCommands(CancellationToken stoppingToken)
    {
        await foreach (var cmd in _stopTypingChannelReader.ReadAllAsync(stoppingToken))
        {
            if (_activeTypingTimersCts.TryRemove(cmd.ChatId, out var cts))
            {
                await cts.CancelAsync();
                cts.Dispose();
                LogRemovedTypingState(cmd.ChatId);
            }
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private async Task RunTypingAsync(long chatId, CancellationToken ct)
    {
        LogTypingActionStarted(chatId);

        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(TypingIntervalMs));

            // отправляем тайпинг статус сразу
            await SendTypingRequest(chatId, ct);

            while (await timer.WaitForNextTickAsync(ct))
            {
                // каждые 4 секунды продлеваем тайпинг статус
                await SendTypingRequest(chatId, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            LogFailedToSendChatActionToChat(chatId, ex);
        }
        finally
        {
            if (_activeTypingTimersCts.TryRemove(chatId, out var cts))
            {
                cts.Dispose();
            }
        }
    }

    private async Task SendTypingRequest(long chatId, CancellationToken ct)
    {
        await _bot.SendChatAction(chatId, ChatAction.Typing, cancellationToken: ct);
        LogTypingActionSent(chatId);
    }

    [LoggerMessage(LogLevel.Information, "Typing Service Started")]
    partial void LogTypingStatusWorkerStarted();

    [LoggerMessage(LogLevel.Information, "Typing Service Stopped")]
    partial void LogTypingStatusWorkerStopped();

    [LoggerMessage(LogLevel.Debug, "Started typing loop for chat {chatId}")]
    partial void LogTypingActionStarted(long chatId);

    [LoggerMessage(LogLevel.Debug, "Stopped typing loop for chat {chatId}")]
    partial void LogRemovedTypingState(long chatId);

    [LoggerMessage(LogLevel.Trace, "Sent typing action to {chatId}")]
    partial void LogTypingActionSent(long chatId);

    [LoggerMessage(LogLevel.Error, "Error sending typing action to {chatId}")]
    partial void LogFailedToSendChatActionToChat(long chatId, Exception ex);
}

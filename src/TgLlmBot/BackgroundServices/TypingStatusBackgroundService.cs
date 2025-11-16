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

    private readonly ChannelReader<StartTypingCommand> _startTypingChannelReader;
    private readonly ChannelReader<StopTypingCommand> _stopTypingChannelReader;
    private readonly TelegramBotClient _bot;
    private readonly ILogger<TypingStatusBackgroundService> _logger;

    private readonly ConcurrentDictionary<ChatThread, CancellationTokenSource> _activeTypingTimersCts = new();

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
            var key = new ChatThread(cmd.ChatId, cmd.ThreadId);

            if (_activeTypingTimersCts.ContainsKey(key))
            {
                continue;
            }

            var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            if (_activeTypingTimersCts.TryAdd(key, cts))
            {
                _ = RunTypingAsync(key, cts.Token);
            }
        }
    }

    private async Task HandleStopTypingCommands(CancellationToken stoppingToken)
    {
        await foreach (var cmd in _stopTypingChannelReader.ReadAllAsync(stoppingToken))
        {
            var key = new ChatThread(cmd.ChatId, cmd.ThreadId);

            if (_activeTypingTimersCts.TryRemove(key, out var cts))
            {
                await cts.CancelAsync();
                cts.Dispose();
                LogRemovedTypingState(cmd.ChatId, cmd.ThreadId);
            }
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private async Task RunTypingAsync(ChatThread info, CancellationToken ct)
    {
        LogTypingActionStarted(info.ChatId, info.ThreadId);

        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(TypingIntervalMs));
            await SendTypingRequest(info, ct);
            while (await timer.WaitForNextTickAsync(ct))
            {
                await SendTypingRequest(info, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            LogFailedToSendChatActionToChat(info.ChatId, info.ThreadId, ex);
        }
        finally
        {
            if (_activeTypingTimersCts.TryRemove(info, out var cts))
            {
                cts.Dispose();
            }
        }
    }

    private async Task SendTypingRequest(ChatThread info, CancellationToken ct)
    {
        await _bot.SendChatAction(info.ChatId, ChatAction.Typing, info.ThreadId, cancellationToken: ct);
        LogTypingActionSent(info.ChatId, info.ThreadId);
    }

    private record struct ChatThread(long ChatId, int? ThreadId);

    [LoggerMessage(LogLevel.Information, "Typing Service Started")]
    partial void LogTypingStatusWorkerStarted();

    [LoggerMessage(LogLevel.Information, "Typing Service Stopped")]
    partial void LogTypingStatusWorkerStopped();

    [LoggerMessage(LogLevel.Debug, "Started typing loop for chat {chatId} thread {threadId}")]
    partial void LogTypingActionStarted(long chatId, int? threadId);

    [LoggerMessage(LogLevel.Debug, "Stopped typing loop for chat {chatId} thread {threadId}")]
    partial void LogRemovedTypingState(long chatId, int? threadId);

    [LoggerMessage(LogLevel.Trace, "Sent typing action to {chatId} thread {threadId}")]
    partial void LogTypingActionSent(long chatId, int? threadId);

    [LoggerMessage(LogLevel.Error, "Error sending typing action to {chatId} thread {threadId}")]
    partial void LogFailedToSendChatActionToChat(long chatId, int? threadId, Exception ex);
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TgLlmBot.Services.Telegram.TypingStatus;

namespace TgLlmBot.BackgroundServices;

public partial class TypingStatusSenderBackgroundService : BackgroundService
{
    private const int TypingIntervalMs = 4_000;
    private const int StaleTimeoutMs = 60_000;

    private readonly ChannelReader<TypingCommand> _commandReader;
    private readonly TelegramBotClient _bot;
    private readonly ILogger<TypingStatusSenderBackgroundService> _logger;

    private readonly ConcurrentDictionary<ChatThreadInfo, ChatTypingState> _activeTypingStates = new();

    public TypingStatusSenderBackgroundService(
        ChannelReader<TypingCommand> commandReader,
        TelegramBotClient bot,
        ILogger<TypingStatusSenderBackgroundService> logger)
    {
        ArgumentNullException.ThrowIfNull(commandReader);
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(logger);
        _commandReader = commandReader;
        _bot = bot;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogTypingStatusWorkerStarted();

        try
        {
            var commandLoopTask = HandleTypingCommands(stoppingToken);
            var updateLoopTask = HandleTypingUpdates(stoppingToken);

            await Task.WhenAll(commandLoopTask, updateLoopTask);

        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            LogTypingStatusWorkerStopped();
            return;
        }

        LogTypingStatusWorkerStopped();
    }

    private async Task HandleTypingCommands(CancellationToken cancellationToken)
    {
        await foreach (var command in _commandReader.ReadAllAsync(cancellationToken))
        {
            switch (command)
            {
                case StartTypingCommand start:
                    HandleStartTyping(start);
                    break;
                case StopTypingCommand stop:
                    await HandleStopTyping(stop, cancellationToken);
                    break;
            }
        }
    }

    private void HandleStartTyping(StartTypingCommand cmd)
    {
        var now = DateTimeOffset.Now;
        var key = cmd.GetChatThreadInfo();

        var typingState = _activeTypingStates.GetOrAdd(key, _ => new(now));

        lock (typingState)
        {
            typingState.PendingActionsCount++;
            typingState.LastActivityTimestamp = now;
        }
    }

    private async Task HandleStopTyping(StopTypingCommand cmd, CancellationToken cancellationToken)
    {
        var chatThreadInfo = cmd.GetChatThreadInfo();

        if (!_activeTypingStates.TryGetValue(chatThreadInfo, out var typingState))
        {
            return;
        }

        int currentCount;
        lock (typingState)
        {
            typingState.PendingActionsCount--;
            currentCount = typingState.PendingActionsCount;
        }

        if (currentCount <= 0)
        {
            _activeTypingStates.TryRemove(chatThreadInfo, out _);
            LogRemovedTypingState(cmd.ChatId, cmd.ThreadId);
        }
        else
        {
            LogTypingStatusSentRightAfterAnswer(cmd.ChatId, cmd.ThreadId);
            await SendTypingAction(chatThreadInfo, cancellationToken);
        }
    }

    private async Task HandleTypingUpdates(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await ProcessTypingUpdates(cancellationToken);
            await Task.Delay(500, cancellationToken);
        }
    }

    private async Task ProcessTypingUpdates(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.Now;
        var keys = _activeTypingStates.Keys.ToArray();

        var sendTypingStatusTasks = new List<Task>();

        foreach (var chatThreadInfo in keys)
        {
            if (!_activeTypingStates.TryGetValue(chatThreadInfo, out var state))
            {
                continue;
            }

            double timeSinceLastActivity;
            double timeSinceLastTypingStatusSent;

            lock (state)
            {
                timeSinceLastActivity = (now - state.LastActivityTimestamp).TotalMilliseconds;
                timeSinceLastTypingStatusSent = (now - state.LastSentTimestamp).TotalMilliseconds;
            }

            if (timeSinceLastActivity > StaleTimeoutMs)
            {
                if (_activeTypingStates.TryRemove(chatThreadInfo, out _))
                {
                    LogStaleTypingStateRemoved(chatThreadInfo.ChatId, chatThreadInfo.ThreadId);
                }
                continue;
            }

            if (timeSinceLastTypingStatusSent >= TypingIntervalMs)
            {
                LogTypingActionSent(chatThreadInfo.ChatId, chatThreadInfo.ThreadId);
                sendTypingStatusTasks.Add(SendTypingAction(chatThreadInfo, cancellationToken));
            }
        }

        await Task.WhenAll(sendTypingStatusTasks);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private async Task SendTypingAction(ChatThreadInfo chatThreadInfo, CancellationToken cancellationToken)
    {
        try
        {
            await _bot.SendChatAction(chatThreadInfo.ChatId, ChatAction.Typing, chatThreadInfo.ThreadId, cancellationToken: cancellationToken);

            if (_activeTypingStates.TryGetValue(chatThreadInfo, out var freshState))
            {
                lock (freshState)
                {
                    freshState.LastSentTimestamp = DateTimeOffset.Now;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            LogFailedToSendChatActionToChat(chatThreadInfo.ChatId, chatThreadInfo.ThreadId, ex);
        }
    }

    private sealed class ChatTypingState
    {
        public int PendingActionsCount { get; set; }
        public DateTimeOffset LastSentTimestamp { get; set; }
        public DateTimeOffset LastActivityTimestamp { get; set; }

        public ChatTypingState(DateTimeOffset now)
        {
            PendingActionsCount = 1;
            LastSentTimestamp = DateTimeOffset.MinValue;
            LastActivityTimestamp = now;
        }
    }

    [LoggerMessage(LogLevel.Information, "Typing Status Sender Background Service started")]
    partial void LogTypingStatusWorkerStarted();

    [LoggerMessage(LogLevel.Information, "Typing Status Sender Background Service finished work")]
    partial void LogTypingStatusWorkerStopped();

    [LoggerMessage(LogLevel.Debug, "Deleted typing state for chat id {chatId} thread id {threadId}")]
    partial void LogRemovedTypingState(long chatId, int? threadId);

    [LoggerMessage(LogLevel.Debug, "Typing status sent right after message sent for chat id {chatId} thread id {threadId}")]
    partial void LogTypingStatusSentRightAfterAnswer(long chatId, int? threadId);

    [LoggerMessage(LogLevel.Warning, "Removed staled typing state for chat id {chatId} thread id {threadId}")]
    partial void LogStaleTypingStateRemoved(long chatId, int? threadId);

    [LoggerMessage(LogLevel.Debug, "Sent typing status for chat id {chatId} thread id {threadId}")]
    partial void LogTypingActionSent(long chatId, int? threadId);

    [LoggerMessage(LogLevel.Error, "Failed sending typing status for chat id {chatId} thread id {threadId}")]
    partial void LogFailedToSendChatActionToChat(long chatId, int? threadId, Exception ex);
}

internal record struct ChatThreadInfo(long ChatId, int? ThreadId);

static file class TypingCommandExtensions
{
    public static ChatThreadInfo GetChatThreadInfo(this TypingCommand cmd) => new(cmd.ChatId, cmd.ThreadId);
}

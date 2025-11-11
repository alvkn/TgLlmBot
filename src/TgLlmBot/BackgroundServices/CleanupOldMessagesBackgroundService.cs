using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TgLlmBot.DataAccess;

namespace TgLlmBot.BackgroundServices;

public partial class CleanupOldMessagesBackgroundService : BackgroundService
{
    private readonly ILogger<CleanupOldMessagesBackgroundService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public CleanupOldMessagesBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<CleanupOldMessagesBackgroundService> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceScopeFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogJobStart();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                LogIterationStart();

                await using (var asyncScope = _serviceScopeFactory.CreateAsyncScope())
                {
                    var dbContext = asyncScope.ServiceProvider.GetRequiredService<BotDbContext>();
                    await CleanupOldMessagesAsync(dbContext, stoppingToken);
                }

                LogIterationComplete();
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                LogJobComplete();
                return;
            }
            catch (Exception ex)
            {
                LogIterationException(ex);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        LogJobComplete();
    }

    private async Task CleanupOldMessagesAsync(BotDbContext dbContext, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var chatIds = await dbContext.ChatHistory
            .AsNoTracking()
            .Select(x => x.ChatId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var chatId in chatIds)
        {
            var cutoffDate = await dbContext.ChatHistory
                .AsNoTracking()
                .Where(x => x.ChatId == chatId)
                .OrderByDescending(x => x.Date)
                .Select(x => x.Date)
                .Skip(200)
                .FirstOrDefaultAsync(cancellationToken);

            if (cutoffDate != default)
            {
                // Удаляем все сообщения старше этой даты для данного чата
                var removedMessages = await dbContext.ChatHistory
                    .AsNoTracking()
                    .Where(x => x.ChatId == chatId && x.Date < cutoffDate)
                    .ExecuteDeleteAsync(cancellationToken);

                LogCleanupComplete(chatId, removedMessages);
            }
            else
            {
                LogCleanupComplete(chatId, 0);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting cleanup job")]
    partial void LogJobStart();

    [LoggerMessage(Level = LogLevel.Information, Message = "Cleanup iteration started")]
    partial void LogIterationStart();

    [LoggerMessage(Level = LogLevel.Information, Message = "Cleanup iteration completed")]
    partial void LogIterationComplete();

    [LoggerMessage(Level = LogLevel.Error, Message = "Cleanup iteration failed with exception")]
    partial void LogIterationException(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Completed cleanup for chat {ChatId}. Removed {RemovedCount} messages")]
    partial void LogCleanupComplete(long chatId, int removedCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Completed cleanup job")]
    partial void LogJobComplete();
}

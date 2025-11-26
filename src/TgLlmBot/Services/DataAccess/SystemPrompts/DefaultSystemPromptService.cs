using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TgLlmBot.DataAccess;
using TgLlmBot.DataAccess.Models;
using TgLlmBot.Models;

namespace TgLlmBot.Services.DataAccess.SystemPrompts;

[SuppressMessage("Style", "IDE0063:Use simple \'using\' statement")]
[SuppressMessage("ReSharper", "ConvertToUsingDeclaration")]
public class DefaultSystemPromptService : ISystemPromptService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public DefaultSystemPromptService(IServiceScopeFactory serviceScopeFactory)
    {
        ArgumentNullException.ThrowIfNull(serviceScopeFactory);
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task SetChatPromptAsync(long chatId, string? systemPrompt, CancellationToken cancellationToken)
    {
        const string sql = $"""
                                INSERT INTO "{nameof(BotDbContext.ChatSystemPrompts)}" ("{nameof(DbChatSystemPrompt.ChatId)}", "{nameof(DbChatSystemPrompt.Prompt)}")
                                VALUES (@{nameof(DbChatSystemPrompt.ChatId)}, @{nameof(DbChatSystemPrompt.Prompt)})
                                ON CONFLICT ("{nameof(DbChatSystemPrompt.ChatId)}") DO UPDATE SET "{nameof(DbChatSystemPrompt.Prompt)}" = @{nameof(DbChatSystemPrompt.Prompt)};
                            """;
        cancellationToken.ThrowIfCancellationRequested();
        await using (var asyncScope = _serviceScopeFactory.CreateAsyncScope())
        {
            var dbContext = asyncScope.ServiceProvider.GetRequiredService<BotDbContext>();
            await dbContext.Database.ExecuteSqlRawAsync(
                sql,
                new NpgsqlParameter($"{nameof(DbChatSystemPrompt.ChatId)}", chatId),
                new NpgsqlParameter($"{nameof(DbChatSystemPrompt.Prompt)}", (object?) systemPrompt?.Trim() ?? DBNull.Value));
        }
    }

    public async Task ResetChatPromptAsync(long chatId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await SetChatPromptAsync(chatId, null, cancellationToken);
    }

    public async Task<Result<string>> GetChatPromptAsync(long chatId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using (var asyncScope = _serviceScopeFactory.CreateAsyncScope())
        {
            var dbContext = asyncScope.ServiceProvider.GetRequiredService<BotDbContext>();
            var dbPrompt = await dbContext.ChatSystemPrompts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ChatId == chatId, cancellationToken);
            if (dbPrompt is not null && !string.IsNullOrWhiteSpace(dbPrompt.Prompt))
            {
                return Result<string>.Success(dbPrompt.Prompt);
            }
        }

        return Result<string>.Fail();
    }

    public async Task SetUserChatPromptAsync(long chatId, long userId, string? systemPrompt, CancellationToken cancellationToken)
    {
        const string sql =
            $"""
             INSERT INTO "{nameof(BotDbContext.PersonalChatSystemPrompts)}" ("{nameof(DbPersonalChatSystemPrompt.ChatId)}", "{nameof(DbPersonalChatSystemPrompt.UserId)}", "{nameof(DbPersonalChatSystemPrompt.Prompt)}")
             VALUES (@{nameof(DbPersonalChatSystemPrompt.ChatId)}, @{nameof(DbPersonalChatSystemPrompt.UserId)}, @{nameof(DbPersonalChatSystemPrompt.Prompt)})
             ON CONFLICT ("{nameof(DbPersonalChatSystemPrompt.ChatId)}", "{nameof(DbPersonalChatSystemPrompt.UserId)}") DO UPDATE SET "{nameof(DbPersonalChatSystemPrompt.Prompt)}" = @{nameof(DbPersonalChatSystemPrompt.Prompt)};
             """;
        cancellationToken.ThrowIfCancellationRequested();
        await using (var asyncScope = _serviceScopeFactory.CreateAsyncScope())
        {
            var dbContext = asyncScope.ServiceProvider.GetRequiredService<BotDbContext>();
            await dbContext.Database.ExecuteSqlRawAsync(
                sql,
                new NpgsqlParameter($"{nameof(DbPersonalChatSystemPrompt.ChatId)}", chatId),
                new NpgsqlParameter($"{nameof(DbPersonalChatSystemPrompt.UserId)}", userId),
                new NpgsqlParameter($"{nameof(DbPersonalChatSystemPrompt.Prompt)}", (object?) systemPrompt?.Trim() ?? DBNull.Value));
        }
    }

    public async Task ResetUserChatPromptAsync(long chatId, long userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await SetUserChatPromptAsync(chatId, userId, null, cancellationToken);
    }

    public async Task<Result<string>> GetUserChatPromptAsync(long chatId, long userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using (var asyncScope = _serviceScopeFactory.CreateAsyncScope())
        {
            var dbContext = asyncScope.ServiceProvider.GetRequiredService<BotDbContext>();
            var dbPrompt = await dbContext.PersonalChatSystemPrompts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ChatId == chatId && x.UserId == userId, cancellationToken);
            if (dbPrompt is not null && !string.IsNullOrWhiteSpace(dbPrompt.Prompt))
            {
                return Result<string>.Success(dbPrompt.Prompt);
            }
        }

        return Result<string>.Fail();
    }
}

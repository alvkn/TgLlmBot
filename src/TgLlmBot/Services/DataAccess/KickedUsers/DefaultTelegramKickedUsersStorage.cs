using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TgLlmBot.DataAccess;
using TgLlmBot.DataAccess.Models;

namespace TgLlmBot.Services.DataAccess.KickedUsers;

[SuppressMessage("ReSharper", "ConvertToUsingDeclaration")]
[SuppressMessage("Style", "IDE0063:Use simple \'using\' statement")]
public class DefaultTelegramKickedUsersStorage : ITelegramKickedUsersStorage
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public DefaultTelegramKickedUsersStorage(IServiceScopeFactory serviceScopeFactory)
    {
        ArgumentNullException.ThrowIfNull(serviceScopeFactory);
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task StoreKickedUserAsync(long chatId, long userId, CancellationToken cancellationToken)
    {
        const string sql = $"""
                                INSERT INTO "{nameof(BotDbContext.KickedUsers)}" ("{nameof(DbKickedUser.ChatId)}", "{nameof(DbKickedUser.UserId)}")
                                VALUES (@{nameof(DbKickedUser.ChatId)}, @{nameof(DbKickedUser.UserId)})
                                ON CONFLICT ("{nameof(DbKickedUser.ChatId)}", "{nameof(DbKickedUser.UserId)}") DO NOTHING;
                            """;
        cancellationToken.ThrowIfCancellationRequested();
        await using (var asyncScope = _serviceScopeFactory.CreateAsyncScope())
        {
            var dbContext = asyncScope.ServiceProvider.GetRequiredService<BotDbContext>();
            await dbContext.Database.ExecuteSqlRawAsync(
                sql,
                new NpgsqlParameter($"{nameof(DbKickedUser.ChatId)}", chatId),
                new NpgsqlParameter($"{nameof(DbKickedUser.UserId)}", userId));
        }
    }

    public async Task RemoveKickedUserAsync(long chatId, long userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using (var asyncScope = _serviceScopeFactory.CreateAsyncScope())
        {
            var dbContext = asyncScope.ServiceProvider.GetRequiredService<BotDbContext>();
            await dbContext.KickedUsers
                .Where(k => k.ChatId == chatId && k.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}

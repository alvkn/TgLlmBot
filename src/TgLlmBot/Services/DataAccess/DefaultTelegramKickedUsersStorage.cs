using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TgLlmBot.DataAccess;

namespace TgLlmBot.Services.DataAccess;

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
        cancellationToken.ThrowIfCancellationRequested();

        await using var asyncScope = _serviceScopeFactory.CreateAsyncScope();
        var dbContext = asyncScope.ServiceProvider.GetRequiredService<BotDbContext>();

        const string sql = """
                               INSERT INTO "KickedUsers" ("ChatId", "Id")
                               VALUES (@ChatId, @userId)
                               ON CONFLICT ("ChatId", "Id") DO NOTHING;
                           """;

        await dbContext.Database.ExecuteSqlRawAsync(
            sql,
            new NpgsqlParameter("ChatId", chatId),
            new NpgsqlParameter("userId", userId));
    }

    public async Task RemoveKickedUserAsync(long chatId, long userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await using var asyncScope = _serviceScopeFactory.CreateAsyncScope();
        var dbContext = asyncScope.ServiceProvider.GetRequiredService<BotDbContext>();

        await dbContext.KickedUsers
            .Where(k => k.ChatId == chatId && k.Id == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}

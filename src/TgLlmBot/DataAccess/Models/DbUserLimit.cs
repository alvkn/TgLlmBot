namespace TgLlmBot.DataAccess.Models;

public class DbUserLimit
{
    public DbUserLimit()
    {
    }

    public DbUserLimit(long chatId, long userId, int limit)
    {
        ChatId = chatId;
        UserId = userId;
        Limit = limit;
    }

    public long ChatId { get; set; }
    public long UserId { get; set; }
    public int Limit { get; set; }
}

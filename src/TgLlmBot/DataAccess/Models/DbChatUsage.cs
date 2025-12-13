using System;

namespace TgLlmBot.DataAccess.Models;

public class DbChatUsage
{
    public DbChatUsage()
    {
    }

    public DbChatUsage(long chatId, long userId, DateTime date, int usage)
    {
        ChatId = chatId;
        UserId = userId;
        Date = date;
        Usage = usage;
    }

    public long ChatId { get; set; }
    public long UserId { get; set; }
    public DateTime Date { get; set; }
    public int Usage { get; set; }
}

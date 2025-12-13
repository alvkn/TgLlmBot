using System;

namespace TgLlmBot.DataAccess.Models;

public class DbChatUsage
{
    public DbChatUsage()
    {
    }

    public DbChatUsage(long chatId, long userId, DateTime date, int used)
    {
        ChatId = chatId;
        UserId = userId;
        Date = date;
        Used = used;
    }

    public long ChatId { get; set; }
    public long UserId { get; set; }
    public DateTime Date { get; set; }
    public int Used { get; set; }
}

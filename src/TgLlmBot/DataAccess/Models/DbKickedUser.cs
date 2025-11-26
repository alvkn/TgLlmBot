namespace TgLlmBot.DataAccess.Models;

public class DbKickedUser
{
    public DbKickedUser()
    {
    }

    public DbKickedUser(long chatId, long userId)
    {
        ChatId = chatId;
        UserId = userId;
    }

    public long ChatId { get; set; }

    public long UserId { get; set; }
}

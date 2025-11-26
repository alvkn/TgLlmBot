namespace TgLlmBot.DataAccess.Models;

public class DbPersonalChatSystemPrompt
{
    public DbPersonalChatSystemPrompt()
    {
    }

    public DbPersonalChatSystemPrompt(long chatId, long userId, string? prompt)
    {
        ChatId = chatId;
        UserId = userId;
        Prompt = prompt;
    }

    public long ChatId { get; set; }
    public long UserId { get; set; }
    public string? Prompt { get; set; }
}

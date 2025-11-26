namespace TgLlmBot.DataAccess.Models;

public class DbChatSystemPrompt
{
    public DbChatSystemPrompt()
    {
    }

    public DbChatSystemPrompt(long chatId, string? prompt)
    {
        ChatId = chatId;
        Prompt = prompt;
    }

    public long ChatId { get; set; }
    public string? Prompt { get; set; }
}

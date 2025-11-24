namespace TgLlmBot.Services.Telegram.Markdown;

public interface ITelegramMarkdownConverter
{
    string[] ConvertToPartedTelegramMarkdown(string normalMarkdown);
    string ConvertToSolidTelegramMarkdown(string normalMarkdown);
}

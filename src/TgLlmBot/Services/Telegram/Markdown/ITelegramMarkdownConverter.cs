namespace TgLlmBot.Services.Telegram.Markdown;

public interface ITelegramMarkdownConverter
{
    string[] ConvertToPartedTelegramMarkdown(string normalMarkdown, int partLengthLimit);
    string ConvertToSolidTelegramMarkdown(string normalMarkdown);
}

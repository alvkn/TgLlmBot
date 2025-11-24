using System;
using System.Collections.Generic;
using System.Linq;
using Markdig;
using Markdig.Extensions.EmphasisExtras;
using TgLlmBot.Services.Telegram.Markdown.Internal;

namespace TgLlmBot.Services.Telegram.Markdown;

/// <summary>
///     Converts Markdown to Telegram MarkdownV2 format with proper escaping.
/// </summary>
public class DefaultTelegramMarkdownConverter : ITelegramMarkdownConverter
{
    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseSpoilers()
        .UseAlertBlocks()
        .UseAutoIdentifiers()
        .UseCustomContainers()
        .UseDefinitionLists()
        .UseEmphasisExtras(EmphasisExtraOptions.Strikethrough)
        .UseGridTables()
        .UseMediaLinks()
        .UsePipeTables()
        .UseListExtras()
        .UseTaskLists()
        .UseAutoLinks()
        .UseGenericAttributes() // Must be last as it is one parser that is modifying other parsers
        .Build();

    public string[] ConvertToPartedTelegramMarkdown(string normalMarkdown)
    {
        var singleDocument = ConvertToSolidTelegramMarkdown(normalMarkdown);
        var parts = SplitDocumentIntoParts(singleDocument, 3000);
        return parts;
    }

    public string ConvertToSolidTelegramMarkdown(string normalMarkdown)
    {
        var document = Markdig.Markdown.Parse(normalMarkdown, MarkdownPipeline);
        var result = new TelegramMarkdownRenderer().Render(document);
        return result;
    }

    private static string[] SplitDocumentIntoParts(string telegramMarkdown, int partLengthLimit)
    {
        if (string.IsNullOrEmpty(telegramMarkdown))
        {
            return Array.Empty<string>();
        }

        var parts = new List<string>();
        var currentPosition = 0;

        while (currentPosition < telegramMarkdown.Length)
        {
            // Если оставшийся текст влезает в лимит целиком — добавляем и выходим
            if (telegramMarkdown.Length - currentPosition <= partLengthLimit)
            {
                parts.Add(telegramMarkdown[currentPosition..]);
                break;
            }

            // Вычисляем индекс, от которого будем искать разделитель назад (конец текущего "чанка")
            var searchEndIndex = currentPosition + partLengthLimit - 1;

            // 1. Приоритет: ищем перенос строки в пределах текущего лимита
            var splitIndex = telegramMarkdown.LastIndexOf('\n', searchEndIndex, partLengthLimit);

            // 2. Если переноса нет, ищем пробел
            if (splitIndex == -1)
            {
                splitIndex = telegramMarkdown.LastIndexOf(' ', searchEndIndex, partLengthLimit);
            }

            if (splitIndex != -1)
            {
                // Разделитель найден. Делим по нему (включая сам символ разделителя в текущую часть)
                var length = splitIndex - currentPosition + 1;
                parts.Add(telegramMarkdown.Substring(currentPosition, length));
                currentPosition += length;
            }
            else
            {
                // 3. Если не нашли удобного места для разрыва, режем ровно по лимиту
                parts.Add(telegramMarkdown.Substring(currentPosition, partLengthLimit));
                currentPosition += partLengthLimit;
            }
        }

        return parts.Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
    }
}

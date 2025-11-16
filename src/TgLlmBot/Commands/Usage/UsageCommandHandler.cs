using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TgLlmBot.CommandDispatcher.Abstractions;
using TgLlmBot.Services.OpenRouter;
using TgLlmBot.Services.OpenRouter.Models;
using TgLlmBot.Services.Telegram.Markdown;

namespace TgLlmBot.Commands.Usage;

public class UsageCommandHandler : AbstractCommandHandler<UsageCommand>
{
    private readonly TelegramBotClient _bot;
    private readonly ITelegramMarkdownConverter _markdownConverter;
    private readonly IOpenRouterKeyUsageProvider _openRouterKeyUsageProvider;
    private readonly TimeProvider _timeProvider;
    private OpenRouterStats _keyStats;
    private DateTimeOffset _lastUpdateAt;


    public UsageCommandHandler(
        TelegramBotClient bot,
        IOpenRouterKeyUsageProvider openRouterKeyUsageProvider,
        TimeProvider timeProvider,
        ITelegramMarkdownConverter markdownConverter)
    {
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(openRouterKeyUsageProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(markdownConverter);
        _bot = bot;
        _openRouterKeyUsageProvider = openRouterKeyUsageProvider;
        _timeProvider = timeProvider;
        _markdownConverter = markdownConverter;
        _lastUpdateAt = DateTimeOffset.MinValue;
        _keyStats = new(0, 0, 0);
    }

    public override async Task HandleAsync(UsageCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        var currentDate = _timeProvider.GetUtcNow();
        var threshold = _lastUpdateAt.AddSeconds(10);
        if (currentDate > threshold)
        {
            _keyStats = await _openRouterKeyUsageProvider.GetOpenRouterKeyUsageAsync(cancellationToken);
            _lastUpdateAt = _timeProvider.GetUtcNow();
        }

        var response = BuildResponseTemplate(_markdownConverter, _keyStats);
        await _bot.SendMessage(
            command.Message.Chat,
            response,
            ParseMode.MarkdownV2,
            new()
            {
                MessageId = command.Message.MessageId
            },
            cancellationToken: cancellationToken);
    }

    private static string BuildResponseTemplate(
        ITelegramMarkdownConverter markdownConverter,
        OpenRouterStats keyStats)
    {
        var builder = new StringBuilder();
        builder.Append("Лимит: ").Append(keyStats.Limit.ToString("F3", CultureInfo.InvariantCulture)).AppendLine(" USD");
        builder.Append("Использовано: ").Append(keyStats.Usage.ToString("F3", CultureInfo.InvariantCulture)).AppendLine(" USD");
        builder.Append("Осталось: ").Append(keyStats.Remaining.ToString("F3", CultureInfo.InvariantCulture)).AppendLine(" USD");
        var rawMarkdown = builder.ToString();
        var optimizedMarkdown = markdownConverter.ConvertToTelegramMarkdown(rawMarkdown);
        return optimizedMarkdown;
    }
}

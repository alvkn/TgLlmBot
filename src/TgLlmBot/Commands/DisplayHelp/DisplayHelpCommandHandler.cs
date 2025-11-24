using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TgLlmBot.CommandDispatcher.Abstractions;
using TgLlmBot.Services.Telegram.Markdown;

namespace TgLlmBot.Commands.DisplayHelp;

public class DisplayHelpCommandHandler : AbstractCommandHandler<DisplayHelpCommand>
{
    private readonly TelegramBotClient _bot;
    private readonly string _response;

    public DisplayHelpCommandHandler(
        DisplayHelpCommandHandlerOptions options,
        TelegramBotClient bot,
        ITelegramMarkdownConverter markdownConverter)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(markdownConverter);
        _response = BuildHelpTemplate(markdownConverter, options.BotName);
        _bot = bot;
    }

    public override async Task HandleAsync(DisplayHelpCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(command);
        await _bot.SendMessage(
            command.Message.Chat,
            _response,
            ParseMode.MarkdownV2,
            new()
            {
                MessageId = command.Message.MessageId
            },
            cancellationToken: cancellationToken);
    }

    private static string BuildHelpTemplate(ITelegramMarkdownConverter markdownConverter, string botName)
    {
        var builder = new StringBuilder();
        builder.Append('`').Append(botName).Append('`').AppendLine(" - префикс для того чтобы задать вопрос LLM");
        builder.AppendLine();
        builder.AppendLine("Например:");
        builder.Append('`').Append(botName).Append(" напиши Hello World на C#").Append('`').AppendLine();
        builder.AppendLine();
        builder.AppendLine("`!ping` - проверка работоспособности бота");
        builder.AppendLine("`!model` - отображает текущую используемую LLM и endpoint к которому идут обращения");
        builder.AppendLine("`!repo` - ссылка на GitHub репозиторий с исходным кодом бота");
        builder.AppendLine("`!usage` - статистика использования API ключа");
        builder.AppendLine("`!rating` - измеряет уровень кринжа и показывает рейтинг щитпостеров");
        builder.AppendLine("`!role` - изменяет системный промпт со стандартного на произвольный (например: `!role общайся на древнерусском`)");
        builder.AppendLine("`!reset` - сбрасывает системный промпт на стандартный");
        var rawMarkdown = builder.ToString();
        var optimizedMarkdown = markdownConverter.ConvertToSolidTelegramMarkdown(rawMarkdown);
        return optimizedMarkdown;
    }
}

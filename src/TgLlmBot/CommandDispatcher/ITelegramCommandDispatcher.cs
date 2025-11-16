using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TgLlmBot.CommandDispatcher;

public interface ITelegramCommandDispatcher
{
    Task HandleMessageAsync(
        Message message,
        UpdateType type,
        CancellationToken cancellationToken);
}

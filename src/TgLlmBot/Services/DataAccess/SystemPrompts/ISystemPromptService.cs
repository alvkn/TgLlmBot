using System.Threading;
using System.Threading.Tasks;
using TgLlmBot.Models;

namespace TgLlmBot.Services.DataAccess.SystemPrompts;

public interface ISystemPromptService
{
    Task SetChatPromptAsync(long chatId, string? systemPrompt, CancellationToken cancellationToken);

    Task ResetChatPromptAsync(long chatId, CancellationToken cancellationToken);

    Task<Result<string>> GetChatPromptAsync(long chatId, CancellationToken cancellationToken);

    Task SetUserChatPromptAsync(long chatId, long userId, string? systemPrompt, CancellationToken cancellationToken);

    Task ResetUserChatPromptAsync(long chatId, long userId, CancellationToken cancellationToken);

    Task<Result<string>> GetUserChatPromptAsync(long chatId, long userId, CancellationToken cancellationToken);
}

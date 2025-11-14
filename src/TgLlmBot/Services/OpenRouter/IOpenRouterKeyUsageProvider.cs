using System.Threading;
using System.Threading.Tasks;
using TgLlmBot.Services.OpenRouter.Models;

namespace TgLlmBot.Services.OpenRouter;

public interface IOpenRouterKeyUsageProvider
{
    Task<OpenRouterStats> GetOpenRouterKeyUsageAsync(CancellationToken cancellationToken);
}

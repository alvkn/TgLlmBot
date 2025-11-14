using System.Diagnostics.CodeAnalysis;

namespace TgLlmBot.Services.OpenAIClient.Costs;

public interface ICostContextStorage
{
    bool TryGetCost([NotNullWhen(true)] out decimal? cost);
    void SetCost(decimal cost);
    void Initialize();
}

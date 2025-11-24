using System.Diagnostics.CodeAnalysis;

namespace TgLlmBot.Services.Llm.Chat;

public interface ICustomChatSystemPromptService
{
    void Set(long chatId, string systemPrompt);

    void Reset(long chatId);

    bool TryGetCustomPrompt(long chatId, [NotNullWhen(true)] out string? systemPrompt);
}

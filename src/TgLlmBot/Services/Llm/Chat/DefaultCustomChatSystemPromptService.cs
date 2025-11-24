using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace TgLlmBot.Services.Llm.Chat;

public class DefaultCustomChatSystemPromptService : ICustomChatSystemPromptService
{
    private readonly ConcurrentDictionary<long, string> _systemPrompts = new();

    public void Set(long chatId, string systemPrompt)
    {
        _systemPrompts[chatId] = systemPrompt;
    }

    public void Reset(long chatId)
    {
        while (_systemPrompts.ContainsKey(chatId))
        {
            _systemPrompts.TryRemove(chatId, out _);
        }
    }

    public bool TryGetCustomPrompt(long chatId, [NotNullWhen(true)] out string? systemPrompt)
    {
        if (_systemPrompts.TryGetValue(chatId, out systemPrompt))
        {
            return true;
        }

        systemPrompt = null;
        return false;
    }
}

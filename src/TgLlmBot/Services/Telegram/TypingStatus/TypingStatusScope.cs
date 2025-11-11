using System;

namespace TgLlmBot.Services.Telegram.TypingStatus;

public sealed class TypingStatusScope : IDisposable
{
    private readonly ITypingStatusService _typingStatusService;
    private readonly long _chatId;
    private readonly int? _threadId;

    public TypingStatusScope(ITypingStatusService typingStatusService, long chatId, int? threadId = default)
    {
        ArgumentNullException.ThrowIfNull(typingStatusService);
        _typingStatusService = typingStatusService;
        _chatId = chatId;
        _threadId = threadId;

        _typingStatusService.StartTyping(_chatId, _threadId);
    }

    public void Dispose()
    {
        _typingStatusService.StopTyping(_chatId, _threadId);
    }
};

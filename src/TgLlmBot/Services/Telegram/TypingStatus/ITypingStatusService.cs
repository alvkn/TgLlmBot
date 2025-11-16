namespace TgLlmBot.Services.Telegram.TypingStatus;

public interface ITypingStatusService
{
    void StartTyping(long chatId, int? threadId);

    void StopTyping(long chatId, int? threadId);
}

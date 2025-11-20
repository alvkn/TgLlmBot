namespace TgLlmBot.Services.Telegram.TypingStatus;

public interface ITypingStatusService
{
    void StartTyping(long chatId);

    void StopTyping(long chatId);
}

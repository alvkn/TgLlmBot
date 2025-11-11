namespace TgLlmBot.Services.Telegram.TypingStatus;

public interface ITypingStatusService
{
    // ReSharper disable once PreferConcreteValueOverDefault
    public void StartTyping(long chatId, int? threadId = default);

    // ReSharper disable once PreferConcreteValueOverDefault
    public void StopTyping(long chatId, int? threadId = default);

    // ReSharper disable once PreferConcreteValueOverDefault
    public TypingStatusScope StartSendTypingStatusScope(long chatId, int? threadId = default);
}

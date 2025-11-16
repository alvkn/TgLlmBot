namespace TgLlmBot.Services.Telegram.TypingStatus;

public record StartTypingCommand(long ChatId, int? ThreadId);

public record StopTypingCommand(long ChatId, int? ThreadId);

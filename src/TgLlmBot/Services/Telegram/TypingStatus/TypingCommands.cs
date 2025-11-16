namespace TgLlmBot.Services.Telegram.TypingStatus;

public record StartTypingCommand(long ChatId);

public record StopTypingCommand(long ChatId);

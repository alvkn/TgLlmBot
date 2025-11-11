namespace TgLlmBot.Services.Telegram.TypingStatus;

// ReSharper disable once PreferConcreteValueOverDefault
public abstract record TypingCommand(long ChatId, int? ThreadId = default);

// ReSharper disable once PreferConcreteValueOverDefault
public record StartTypingCommand(long ChatId, int? ThreadId = default) : TypingCommand(ChatId, ThreadId);

// ReSharper disable once PreferConcreteValueOverDefault
public record StopTypingCommand(long ChatId, int? ThreadId = default) : TypingCommand(ChatId, ThreadId);

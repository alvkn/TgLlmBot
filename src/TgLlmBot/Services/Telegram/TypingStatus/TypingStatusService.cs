using System;
using System.Threading.Channels;

namespace TgLlmBot.Services.Telegram.TypingStatus;

public class TypingStatusService : ITypingStatusService
{
    private readonly ChannelWriter<StartTypingCommand> _startTypingCommandWriter;
    private readonly ChannelWriter<StopTypingCommand> _stopTypingCommandWriter;

    public TypingStatusService(
        ChannelWriter<StartTypingCommand> startTypingCommandWriter,
        ChannelWriter<StopTypingCommand> stopTypingCommandWriter)
    {
        ArgumentNullException.ThrowIfNull(startTypingCommandWriter);
        ArgumentNullException.ThrowIfNull(stopTypingCommandWriter);
        _startTypingCommandWriter = startTypingCommandWriter;
        _stopTypingCommandWriter = stopTypingCommandWriter;
    }

    public void StartTyping(long chatId)
    {
        _startTypingCommandWriter.TryWrite(new(chatId));
    }

    public void StopTyping(long chatId)
    {
        _stopTypingCommandWriter.TryWrite(new(chatId));
    }
}

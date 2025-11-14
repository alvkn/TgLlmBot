using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TgLlmBot.CommandDispatcher.Abstractions;

namespace TgLlmBot.Commands.ChatWithLlm;

public class ChatWithLlmCommandHandler : AbstractCommandHandler<ChatWithLlmCommand>
{
    private readonly ChannelWriter<ChatWithLlmCommand> _channelWriter;

    public ChatWithLlmCommandHandler(ChannelWriter<ChatWithLlmCommand> channelWriter)
    {
        ArgumentNullException.ThrowIfNull(channelWriter);
        _channelWriter = channelWriter;
    }

    public override async Task HandleAsync(ChatWithLlmCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        await _channelWriter.WriteAsync(command, cancellationToken);
    }
}

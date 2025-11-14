using System.Threading;
using System.Threading.Tasks;

namespace TgLlmBot.CommandDispatcher.Abstractions;

public abstract class AbstractCommandHandler<TCommand> where TCommand : AbstractCommand
{
    public abstract Task HandleAsync(TCommand command, CancellationToken cancellationToken);
}

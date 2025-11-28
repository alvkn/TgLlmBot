using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Client;

namespace TgLlmBot.Services.Mcp.Clients.Context7;

public interface IContext7McpClientFactory
{
    Task<McpClient> CreateAsync(CancellationToken cancellationToken);
}

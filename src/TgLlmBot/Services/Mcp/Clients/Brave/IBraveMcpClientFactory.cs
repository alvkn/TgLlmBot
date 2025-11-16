using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Client;

namespace TgLlmBot.Services.Mcp.Clients.Brave;

public interface IBraveMcpClientFactory
{
    Task<McpClient> CreateAsync(CancellationToken cancellationToken);
}

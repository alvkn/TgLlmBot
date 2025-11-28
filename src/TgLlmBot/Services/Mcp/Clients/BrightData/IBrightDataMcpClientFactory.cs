using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Client;

namespace TgLlmBot.Services.Mcp.Clients.BrightData;

public interface IBrightDataMcpClientFactory
{
    Task<McpClient> CreateAsync(CancellationToken cancellationToken);
}

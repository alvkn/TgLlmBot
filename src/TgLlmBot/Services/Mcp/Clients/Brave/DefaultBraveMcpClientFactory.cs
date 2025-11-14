using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace TgLlmBot.Services.Mcp.Clients.Brave;

public class DefaultBraveMcpClientFactory : IBraveMcpClientFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly DefaultBraveMcpClientFactoryOptions _options;

    public DefaultBraveMcpClientFactory(
        DefaultBraveMcpClientFactoryOptions options,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _options = options;
        _loggerFactory = loggerFactory;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
    public async Task<McpClient> CreateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var client = await McpClient.CreateAsync(new StdioClientTransport(new()
            {
                Command = "docker",
                EnvironmentVariables = new Dictionary<string, string?>
                {
                    { "BRAVE_API_KEY", _options.ApiKey },
                    { "BRAVE_MCP_ENABLED_TOOLS", "brave_web_search brave_news_search" }
                },
                Arguments = new List<string>
                {
                    "run",
                    "-i",
                    "--rm",
                    "-e",
                    "BRAVE_API_KEY",
                    "-e",
                    "BRAVE_MCP_ENABLED_TOOLS",
                    "docker.io/mcp/brave-search"
                }
            }, _loggerFactory),
            null,
            _loggerFactory,
            cancellationToken);
        return client;
    }
}

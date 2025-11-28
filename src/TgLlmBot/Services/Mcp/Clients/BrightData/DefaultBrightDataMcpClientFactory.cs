using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace TgLlmBot.Services.Mcp.Clients.BrightData;

public class DefaultBrightDataMcpClientFactory : IBrightDataMcpClientFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly DefaultBrightDataMcpClientFactoryOptions _options;

    public DefaultBrightDataMcpClientFactory(
        DefaultBrightDataMcpClientFactoryOptions options,
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
                Command = "npx",
                EnvironmentVariables = new Dictionary<string, string?>
                {
                    { "API_TOKEN", _options.ApiKey }
                },
                Arguments = new List<string>
                {
                    "-y",
                    "@brightdata/mcp"
                }
            }, _loggerFactory),
            null,
            _loggerFactory,
            cancellationToken);
        return client;
    }
}

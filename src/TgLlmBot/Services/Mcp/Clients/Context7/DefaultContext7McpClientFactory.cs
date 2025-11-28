using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace TgLlmBot.Services.Mcp.Clients.Context7;

public class DefaultContext7McpClientFactory : IContext7McpClientFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly DefaultContext7McpClientFactoryOptions _options;

    public DefaultContext7McpClientFactory(
        DefaultContext7McpClientFactoryOptions options,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _options = options;
        _loggerFactory = loggerFactory;
    }

    public async Task<McpClient> CreateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var client = await McpClient.CreateAsync(new StdioClientTransport(new()
            {
                Command = "npx",
                Arguments = new List<string>
                {
                    "-y",
                    "@upstash/context7-mcp",
                    "--api-key",
                    _options.ApiKey
                }
            }, _loggerFactory),
            null,
            _loggerFactory,
            cancellationToken);
        return client;
    }
}

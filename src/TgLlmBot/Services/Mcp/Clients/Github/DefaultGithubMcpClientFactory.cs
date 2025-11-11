using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using TgLlmBot.Services.Mcp.Enums;

namespace TgLlmBot.Services.Mcp.Clients.Github;

public class DefaultGithubMcpClientFactory : IGithubMcpClientFactory
{
    public const string GithubHttpClientName = $"http-client-factory-{nameof(McpClientName.Github)}";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;

    private readonly DefaultGithubMcpClientFactoryOptions _options;

    public DefaultGithubMcpClientFactory(
        DefaultGithubMcpClientFactoryOptions options,
        ILoggerFactory loggerFactory,
        IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        _options = options;
        _loggerFactory = loggerFactory;
        _httpClientFactory = httpClientFactory;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
    public async Task<McpClient> CreateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var client = await McpClient.CreateAsync(new StdioClientTransport(new()
            {
                Command = _options.Command,
                EnvironmentVariables = new Dictionary<string, string?>
                {
                    { "GITHUB_PERSONAL_ACCESS_TOKEN", _options.GithubPat }
                },
                Arguments = new List<string>
                {
                    "stdio"
                },
                WorkingDirectory = _options.WorkingDirectory
            }, _loggerFactory),
            null,
            _loggerFactory,
            cancellationToken);
        return client;
    }
}

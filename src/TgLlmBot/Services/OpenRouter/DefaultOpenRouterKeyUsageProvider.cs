using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using TgLlmBot.Services.OpenRouter.Models;

namespace TgLlmBot.Services.OpenRouter;

public class DefaultOpenRouterKeyUsageProvider : IOpenRouterKeyUsageProvider
{
    private static readonly Uri GetKeyInfoUri = new("https://openrouter.ai/api/v1/key", UriKind.Absolute);
    private readonly HttpClient _httpClient;
    private readonly DefaultOpenRouterKeyUsageProviderOptions _options;

    public DefaultOpenRouterKeyUsageProvider(HttpClient httpClient, DefaultOpenRouterKeyUsageProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<OpenRouterStats> GetOpenRouterKeyUsageAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using (var request = new HttpRequestMessage(HttpMethod.Get, GetKeyInfoUri))
        {
            request.Headers.Authorization = new("Bearer", _options.ApiKey);
            using (var response = await _httpClient.SendAsync(
                       request,
                       HttpCompletionOption.ResponseHeadersRead,
                       cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                var jsonResponse = await response.Content.ReadFromJsonAsync<InternalResponse>(cancellationToken);
                if (jsonResponse is null)
                {
                    throw new InvalidOperationException();
                }

                return new(
                    jsonResponse.Data.Usage,
                    jsonResponse.Data.LimitRemaining,
                    jsonResponse.Data.Limit);
            }
        }
    }

    private sealed class InternalResponse
    {
        public InternalResponse(InternalData data)
        {
            Data = data;
        }

        [JsonPropertyName("data")]
        public InternalData Data { get; }
    }

    private sealed class InternalData
    {
        public InternalData(decimal limit, decimal limitRemaining, decimal usage)
        {
            Limit = limit;
            LimitRemaining = limitRemaining;
            Usage = usage;
        }

        [JsonPropertyName("limit")]
        public decimal Limit { get; }

        [JsonPropertyName("limit_remaining")]
        public decimal LimitRemaining { get; }

        [JsonPropertyName("usage")]
        public decimal Usage { get; }
    }
}

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using TgLlmBot.Services.OpenAIClient.Costs;

namespace TgLlmBot.Services.OpenAIClient.HttpClient.DelegatingHandlers;

public class ModifyChatCompletionsRequestDelegatingHandler : DelegatingHandler
{
    private static readonly Uri ChatCompletionsUri = new("https://openrouter.ai/api/v1/chat/completions", UriKind.Absolute);

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.General)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = false
    };

    private static readonly JsonWriterOptions JsonWriterOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Indented = false,
        SkipValidation = false
    };

    private readonly ICostContextStorage _costStorage;

    public ModifyChatCompletionsRequestDelegatingHandler(ICostContextStorage costStorage)
    {
        ArgumentNullException.ThrowIfNull(costStorage);
        _costStorage = costStorage;
    }

    protected override HttpResponseMessage Send(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.RequestUri?.Equals(ChatCompletionsUri) is true
            && request.Method == HttpMethod.Post)
        {
            var response = await PatchRequestAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var binaryResponse = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                var jsonString = Encoding.UTF8.GetString(binaryResponse).Trim();
                var internalResponse = JsonSerializer.Deserialize<InternalResponse>(jsonString, JsonSerializerOptions);
                if (internalResponse is not null)
                {
                    _costStorage.SetCost(internalResponse.Usage.Cost);
                }

                response.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            }

            return response;
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> PatchRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Content is null)
        {
            throw new InvalidOperationException("No content");
        }

        var rawUtf8Bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
        var rawBodyString = Encoding.UTF8.GetString(rawUtf8Bytes).Trim();
        var doc = JsonSerializer.Deserialize<JsonDocument>(rawBodyString, JsonSerializerOptions);
        if (doc?.RootElement.ValueKind is not JsonValueKind.Object)
        {
            throw new InvalidOperationException("Invalid json");
        }

        byte[] patchedRequest;
        using (var outputStream = new MemoryStream(131072))
        {
            await using (var writer = new Utf8JsonWriter(outputStream, JsonWriterOptions))
            {
                writer.WriteStartObject();
                foreach (var property in doc.RootElement.EnumerateObject())
                {
                    property.WriteTo(writer);
                }

                writer.WritePropertyName("usage");
                writer.WriteStartObject();
                writer.WriteBoolean("include", true);
                writer.WriteEndObject();
                writer.WriteEndObject();
                await writer.FlushAsync(cancellationToken);
                patchedRequest = outputStream.ToArray();
            }
        }

        var rawPatchedRequest = Encoding.UTF8.GetString(patchedRequest);
        using (var content = new StringContent(rawPatchedRequest, Encoding.UTF8, "application/json"))
        {
            request.Content = content;
            return await base.SendAsync(request, cancellationToken);
        }
    }

    private sealed class InternalResponse
    {
        public InternalResponse(InternalUsage usage)
        {
            Usage = usage;
        }

        [JsonPropertyName("usage")]
        public InternalUsage Usage { get; }
    }

    private sealed class InternalUsage
    {
        public InternalUsage(long promptTokens, long completionTokens, long totalTokens, decimal cost)
        {
            PromptTokens = promptTokens;
            CompletionTokens = completionTokens;
            TotalTokens = totalTokens;
            Cost = cost;
        }

        [JsonPropertyName("prompt_tokens")]
        public long PromptTokens { get; }

        [JsonPropertyName("completion_tokens")]
        public long CompletionTokens { get; }

        [JsonPropertyName("total_tokens")]
        public long TotalTokens { get; }

        [JsonPropertyName("cost")]
        public decimal Cost { get; }
    }
}

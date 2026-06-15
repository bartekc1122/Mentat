using System.ClientModel;
using OpenAI.Embeddings;

namespace Mentat.Infrastructure.Embeddings;

/// <summary>Generuje embeddingi przez OpenAI (model text-embedding-3-small).</summary>
public sealed class EmbeddingService : IEmbeddingService
{
    private const string Model = "text-embedding-3-small";

    private readonly EmbeddingClient _client;

    public EmbeddingService(string apiKey)
    {
        _client = new EmbeddingClient(Model, new ApiKeyCredential(apiKey));
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        ClientResult<OpenAIEmbedding> result = await _client.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
        return result.Value.ToFloats().ToArray();
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
            return [];

        ClientResult<OpenAIEmbeddingCollection> result =
            await _client.GenerateEmbeddingsAsync(texts, cancellationToken: cancellationToken);

        return [.. result.Value.Select(e => e.ToFloats().ToArray())];
    }
}

using Mentat.Infrastructure.Embeddings;
using Mentat.Infrastructure.Storage;

namespace Mentat.Infrastructure.Search;

/// <summary>Pojedynczy wynik wyszukiwania — element (informacja/zadanie) z dopasowaniem.</summary>
public sealed record SearchResult(Note Note, double Score)
{
    public int MeetingId => Note.MeetingId;
    public string Kind => Note.Kind;
    public string Content => Note.Content;

    /// <summary>Dosłowny cytat z transkryptu, w którym element padł.</summary>
    public string Quote => Note.Quote;
}

/// <summary>
/// Wyszukiwanie semantyczne po zapisanych notatkach: embed zapytania (OpenAI) + podobieństwo
/// kosinusowe liczone lokalnie. Pozwala znaleźć wszystkie rozmowy na dany temat w obrębie projektu.
/// </summary>
public sealed class SemanticSearchService
{
    private readonly MeetingDatabase _database;
    private readonly IEmbeddingService _embeddings;

    public SemanticSearchService(MeetingDatabase database, IEmbeddingService embeddings)
    {
        _database = database;
        _embeddings = embeddings;
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        int? projectId = null,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        float[] queryVector = await _embeddings.EmbedAsync(query, cancellationToken);

        List<Note> notes = await _database.GetNotesByProjectAsync(projectId);

        return [.. notes
            .Where(n => n.Embedding is { Length: > 0 })
            .Select(n => new SearchResult(n, VectorMath.CosineSimilarity(queryVector, VectorMath.FromBytes(n.Embedding!))))
            .OrderByDescending(r => r.Score)
            .Take(topK)];
    }
}

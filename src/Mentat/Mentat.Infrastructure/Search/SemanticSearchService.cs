using Mentat.Infrastructure.Embeddings;
using Mentat.Infrastructure.Storage;

namespace Mentat.Infrastructure.Search;

/// <summary>Pojedynczy wynik wyszukiwania — element notatki z dopasowaniem i linkami do cytatów.</summary>
public sealed record SearchResult(Note Note, double Score)
{
    public int MeetingId => Note.MeetingId;
    public string Title => Note.Title;
    public string Body => Note.Body;

    /// <summary>Refy źródłowych wypowiedzi (np. "u3,u4") — linki do skoku do cytatu/rozmowy.</summary>
    public string[] SourceRefs =>
        Note.SourceRefs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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

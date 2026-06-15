using SQLite;

namespace Mentat.Infrastructure.Storage;

/// <summary>
/// Ujednolicona tabela na wyniki pipeline'u: temat, decyzja lub zadanie (rozróżniane przez
/// <see cref="Type"/>). Każdy wiersz ma własne linki do źródłowych wypowiedzi (<see cref="SourceRefs"/>)
/// oraz embedding pod wyszukiwanie semantyczne.
/// </summary>
[Table("notes")]
public sealed class Note
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int MeetingId { get; set; }

    /// <summary>Zdenormalizowane z <c>Meeting</c> — pozwala filtrować wyszukiwanie po projekcie bez JOIN-a.</summary>
    [Indexed]
    public int ProjectId { get; set; }

    /// <summary>"topic" | "decision" | "action".</summary>
    public string Type { get; set; } = NoteTypes.Topic;

    public string Title { get; set; } = "";

    /// <summary>Podsumowanie tematu / treść decyzji / opis zadania.</summary>
    public string Body { get; set; } = "";

    // Tylko dla zadań (Type == "action"); w pozostałych przypadkach null.
    public string? Owner { get; set; }
    public string? Deadline { get; set; }
    public string? Blocker { get; set; }

    /// <summary>Refy źródłowych wypowiedzi (np. "u3,u4,u7") — linki/cytaty pokazywane pod elementem.</summary>
    public string SourceRefs { get; set; } = "";

    /// <summary>Embedding tekstu elementu (float[] zapisany jako bajty); null dla zadań, które nie są embedowane.</summary>
    public byte[]? Embedding { get; set; }

    public int EmbeddingDim { get; set; }
}

public static class NoteTypes
{
    public const string Topic = "topic";
    public const string Decision = "decision";
    public const string Action = "action";
}

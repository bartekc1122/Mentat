using SQLite;

namespace Mentat.Infrastructure.Storage;

/// <summary>
/// Pojedynczy element wyodrębniony z rozmowy: 'informacja' albo 'zadanie' (pole <see cref="Kind"/>).
/// Niesie dosłowny cytat z transkryptu (<see cref="Quote"/>) oraz embedding pod wyszukiwanie semantyczne.
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

    /// <summary>"informacja" | "zadanie".</summary>
    public string Kind { get; set; } = ItemKinds.Informacja;

    /// <summary>Treść elementu (nowy fakt / wykonane zadanie lub czynność do wykonania).</summary>
    public string Content { get; set; } = "";

    // Tylko dla zadań (Kind == "zadanie"); w pozostałych przypadkach null.
    public string? Owner { get; set; }
    public string? Deadline { get; set; }

    /// <summary>Dokładny, dosłowny cytat z transkryptu, w którym element padł.</summary>
    public string Quote { get; set; } = "";

    /// <summary>Embedding treści elementu (float[] zapisany jako bajty) pod wyszukiwanie semantyczne.</summary>
    public byte[]? Embedding { get; set; }

    public int EmbeddingDim { get; set; }
}

public static class ItemKinds
{
    public const string Informacja = "informacja";
    public const string Zadanie = "zadanie";
}

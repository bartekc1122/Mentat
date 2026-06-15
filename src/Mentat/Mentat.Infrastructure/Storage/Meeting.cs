using SQLite;

namespace Mentat.Infrastructure.Storage;

/// <summary>
/// Pojedyncze spotkanie w obrębie projektu. Surowe wypowiedzi (z refami) trzymamy w
/// <see cref="UtterancesJson"/> — są zawsze ładowane razem ze spotkaniem przy pokazywaniu
/// cytatu, więc nie potrzebują osobnej tabeli.
/// </summary>
[Table("meetings")]
public sealed class Meeting
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int ProjectId { get; set; }

    public string Title { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    /// <summary>Zserializowana lista <c>Utterance</c> ({ ref, speaker, text }) — źródło cytatów dla linków.</summary>
    public string UtterancesJson { get; set; } = "[]";
}

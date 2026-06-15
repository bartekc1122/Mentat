using SQLite;

namespace Mentat.Infrastructure.Storage;

/// <summary>Projekt grupujący spotkania (np. klient, produkt, zespół).</summary>
[Table("projects")]
public sealed class Project
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public DateTime CreatedAt { get; set; }
}

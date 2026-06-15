using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Mentat.Infrastructure.Embeddings;
using Mentat.Infrastructure.LLM;
using Mentat.Infrastructure.Models;
using Mentat.Infrastructure.Search;
using Mentat.Infrastructure.Storage;

namespace Mentat.Infrastructure.Pipeline;

public sealed record ProcessResult(Meeting Meeting, int NoteCount);

/// <summary>
/// Funkcja docelowa: przyjmuje gotowy tekst z rolami i wykonuje cały pipeline —
/// split → okna z nakładaniem → ekstrakcja elementów per okno (LLM) → konsolidacja (LLM) →
/// embeddingi → zapis spotkania i elementów w SQLite.
/// </summary>
public sealed class MeetingProcessor
{
    private readonly MeetingDatabase _database;
    private readonly NoteExtractor _extractor;
    private readonly IEmbeddingService _embeddings;
    private readonly int _windowSize;
    private readonly int _windowOverlap;

    public MeetingProcessor(
        MeetingDatabase database,
        NoteExtractor extractor,
        IEmbeddingService embeddings,
        int windowSize = Windowing.DefaultSize,
        int windowOverlap = Windowing.DefaultOverlap)
    {
        _database = database;
        _extractor = extractor;
        _embeddings = embeddings;
        _windowSize = windowSize;
        _windowOverlap = windowOverlap;
    }

    public async Task<ProcessResult> ProcessAsync(string transcript, int projectId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Utterance> utterances = UtteranceSplitter.Split(transcript);
        if (utterances.Count == 0)
            throw new ArgumentException("Transkrypt jest pusty lub nie zawiera wypowiedzi.", nameof(transcript));

        // Etap 1: ekstrakcja per okno (z nakładaniem). Błąd jednego okna nie przerywa całości.
        var windowResults = new List<ItemsPayload>();
        foreach (IReadOnlyList<Utterance> window in Windowing.Window(utterances, _windowSize, _windowOverlap))
        {
            try
            {
                ItemsPayload payload = await _extractor.ExtractWindowAsync(BuildWindowText(window), cancellationToken);
                windowResults.Add(payload);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MeetingProcessor] Pominięto okno z powodu błędu ekstrakcji: {ex.Message}");
            }
        }

        // Etap 2: konsolidacja (scalanie duplikatów z nakładających się okien).
        ItemsPayload consolidated = windowResults.Count switch
        {
            0 => new ItemsPayload([]),
            1 => windowResults[0],
            _ => await _extractor.ConsolidateAsync(windowResults, cancellationToken),
        };

        // Zapis spotkania z timestampem (kiedy się odbyło). Tytuł z pierwszego elementu, jeśli jest.
        DateTime now = DateTime.Now;
        var meeting = new Meeting
        {
            ProjectId = projectId,
            Title = BuildTitle(consolidated, now),
            CreatedAt = now,
            UtterancesJson = JsonSerializer.Serialize(utterances),
        };
        meeting.Id = await _database.AddMeetingAsync(meeting);

        // Etap 3: budowa elementów + embeddingi (każdy element pod wyszukiwanie semantyczne).
        List<Note> notes = BuildNotes(consolidated, meeting);
        await EmbedNotesAsync(notes, cancellationToken);

        await _database.AddNotesAsync(notes);
        return new ProcessResult(meeting, notes.Count);
    }

    private static string BuildWindowText(IReadOnlyList<Utterance> window)
    {
        // Bez prefiksów [uN] — model ma zwracać dosłowne cytaty z czystego tekstu 'Mówca: tekst'.
        var sb = new StringBuilder();
        foreach (Utterance u in window)
            sb.Append(u.Speaker).Append(": ").AppendLine(u.Text);
        return sb.ToString();
    }

    private static string BuildTitle(ItemsPayload payload, DateTime now)
    {
        string? first = payload.Items.FirstOrDefault()?.Content;
        if (string.IsNullOrWhiteSpace(first))
            return $"Spotkanie {now:yyyy-MM-dd HH:mm}";

        first = first.Trim();
        return first.Length <= 60 ? first : first[..60].TrimEnd() + "…";
    }

    private static List<Note> BuildNotes(ItemsPayload payload, Meeting meeting) =>
    [
        .. payload.Items.Select(item => new Note
        {
            MeetingId = meeting.Id,
            ProjectId = meeting.ProjectId,
            Kind = item.Kind,
            Content = item.Content,
            Owner = item.Owner,
            Deadline = item.Deadline,
            Quote = item.Quote,
        })
    ];

    /// <summary>Embeduje treść każdego elementu jednym wywołaniem batch — pod wyszukiwanie semantyczne.</summary>
    private async Task EmbedNotesAsync(List<Note> notes, CancellationToken cancellationToken)
    {
        if (notes.Count == 0)
            return;

        var texts = notes.Select(n => n.Content).ToList();
        IReadOnlyList<float[]> vectors = await _embeddings.EmbedBatchAsync(texts, cancellationToken);

        for (int i = 0; i < notes.Count && i < vectors.Count; i++)
        {
            notes[i].Embedding = VectorMath.ToBytes(vectors[i]);
            notes[i].EmbeddingDim = vectors[i].Length;
        }
    }
}

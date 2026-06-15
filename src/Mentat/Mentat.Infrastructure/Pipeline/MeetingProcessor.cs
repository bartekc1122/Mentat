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
/// split → okna z nakładaniem → ekstrakcja per okno (LLM) → konsolidacja (LLM) →
/// embeddingi → zapis spotkania i notatek w SQLite.
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
        var windowResults = new List<NotesPayload>();
        foreach (IReadOnlyList<Utterance> window in Windowing.Window(utterances, _windowSize, _windowOverlap))
        {
            try
            {
                NotesPayload payload = await _extractor.ExtractWindowAsync(BuildWindowText(window), cancellationToken);
                windowResults.Add(payload);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MeetingProcessor] Pominięto okno z powodu błędu ekstrakcji: {ex.Message}");
            }
        }

        // Etap 2: konsolidacja (scalanie duplikatów z nakładających się okien).
        NotesPayload consolidated = windowResults.Count switch
        {
            0 => new NotesPayload([], [], []),
            1 => windowResults[0],
            _ => await _extractor.ConsolidateAsync(windowResults, cancellationToken),
        };

        // Zapis spotkania (tytuł z pierwszego tematu, jeśli jest).
        var meeting = new Meeting
        {
            ProjectId = projectId,
            Title = consolidated.Topics.FirstOrDefault()?.Title ?? $"Spotkanie {DateTime.Now:yyyy-MM-dd HH:mm}",
            CreatedAt = DateTime.UtcNow,
            UtterancesJson = JsonSerializer.Serialize(utterances),
        };
        meeting.Id = await _database.AddMeetingAsync(meeting);

        // Etap 3: budowa notatek + embeddingi (tematy i decyzje; zadania linkują się przez temat).
        List<Note> notes = BuildNotes(consolidated, meeting);
        await EmbedNotesAsync(notes, cancellationToken);

        await _database.AddNotesAsync(notes);
        return new ProcessResult(meeting, notes.Count);
    }

    private static string BuildWindowText(IReadOnlyList<Utterance> window)
    {
        var sb = new StringBuilder();
        foreach (Utterance u in window)
            sb.Append('[').Append(u.Ref).Append("] ").Append(u.Speaker).Append(": ").AppendLine(u.Text);
        return sb.ToString();
    }

    private static List<Note> BuildNotes(NotesPayload payload, Meeting meeting)
    {
        var notes = new List<Note>();

        foreach (ExtractedTopic t in payload.Topics)
            notes.Add(NewNote(meeting, NoteTypes.Topic, t.Title, t.Summary, t.SourceRefs));

        foreach (ExtractedDecision d in payload.Decisions)
            notes.Add(NewNote(meeting, NoteTypes.Decision, d.TopicTitle ?? "", d.Text, d.SourceRefs));

        foreach (ExtractedAction a in payload.ActionItems)
        {
            Note note = NewNote(meeting, NoteTypes.Action, a.TopicTitle ?? "", a.Task, a.SourceRefs);
            note.Owner = a.Owner;
            note.Deadline = a.Deadline;
            note.Blocker = a.Blocker;
            notes.Add(note);
        }

        return notes;
    }

    private static Note NewNote(Meeting meeting, string type, string title, string body, string[] sourceRefs) => new()
    {
        MeetingId = meeting.Id,
        ProjectId = meeting.ProjectId,
        Type = type,
        Title = title,
        Body = body,
        SourceRefs = string.Join(",", sourceRefs),
    };

    /// <summary>Embeduje tematy i decyzje jednym wywołaniem batch; zadania zostają bez embeddingu.</summary>
    private async Task EmbedNotesAsync(List<Note> notes, CancellationToken cancellationToken)
    {
        List<Note> toEmbed = [.. notes.Where(n => n.Type is NoteTypes.Topic or NoteTypes.Decision)];
        if (toEmbed.Count == 0)
            return;

        var texts = toEmbed.Select(n => string.IsNullOrEmpty(n.Title) ? n.Body : $"{n.Title}\n{n.Body}").ToList();
        IReadOnlyList<float[]> vectors = await _embeddings.EmbedBatchAsync(texts, cancellationToken);

        for (int i = 0; i < toEmbed.Count && i < vectors.Count; i++)
        {
            toEmbed[i].Embedding = VectorMath.ToBytes(vectors[i]);
            toEmbed[i].EmbeddingDim = vectors[i].Length;
        }
    }
}

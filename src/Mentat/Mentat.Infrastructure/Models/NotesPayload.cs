using System.Text.Json.Serialization;

namespace Mentat.Infrastructure.Models;

/// <summary>
/// Wspólny kształt wyjścia LLM dla etapu 1 (ekstrakcja per okno) i etapu 2 (konsolidacja).
/// Każdy element niesie własne <c>source_refs</c> (refy "u{n}") — LLM sam przypisuje, które
/// wypowiedzi są źródłem danego tematu/decyzji/zadania.
/// </summary>
public sealed record NotesPayload(
    [property: JsonPropertyName("topics")] ExtractedTopic[] Topics,
    [property: JsonPropertyName("decisions")] ExtractedDecision[] Decisions,
    [property: JsonPropertyName("action_items")] ExtractedAction[] ActionItems);

public sealed record ExtractedTopic(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("source_refs")] string[] SourceRefs);

public sealed record ExtractedDecision(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("topic_title")] string? TopicTitle,
    [property: JsonPropertyName("source_refs")] string[] SourceRefs);

public sealed record ExtractedAction(
    [property: JsonPropertyName("task")] string Task,
    [property: JsonPropertyName("owner")] string? Owner,
    [property: JsonPropertyName("deadline")] string? Deadline,
    [property: JsonPropertyName("blocker")] string? Blocker,
    [property: JsonPropertyName("topic_title")] string? TopicTitle,
    [property: JsonPropertyName("source_refs")] string[] SourceRefs);

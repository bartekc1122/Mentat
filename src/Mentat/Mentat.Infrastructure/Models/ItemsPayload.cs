using System.Text.Json.Serialization;

namespace Mentat.Infrastructure.Models;

/// <summary>
/// Wyjście LLM (etap 1 ekstrakcji i etap 2 konsolidacji): płaska lista elementów.
/// Każdy element to 'informacja' albo 'zadanie' i niesie dosłowny cytat z tekstu.
/// </summary>
public sealed record ItemsPayload(
    [property: JsonPropertyName("items")] ExtractedItem[] Items);

public sealed record ExtractedItem(
    [property: JsonPropertyName("kind")] string Kind,        // "informacja" | "zadanie"
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("owner")] string? Owner,     // tylko dla 'zadanie'
    [property: JsonPropertyName("deadline")] string? Deadline, // tylko dla 'zadanie'
    [property: JsonPropertyName("quote")] string Quote);     // dosłowny cytat z transkryptu

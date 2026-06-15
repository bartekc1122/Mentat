using System.Text.Json.Serialization;

namespace Mentat.Infrastructure.Models
{
    public record ActionItem(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("task")] string Task,
        [property: JsonPropertyName("owner")] string? Owner,
        [property: JsonPropertyName("deadline")] string? Deadline,
        [property: JsonPropertyName("blocker")] string? Blocker
    );

    public record MeetingNotes(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("date")] string? Date,
        [property: JsonPropertyName("attendees")] string[] Attendees,
        [property: JsonPropertyName("key_points")] string[] KeyPoints,
        [property: JsonPropertyName("decisions")] string[] Decisions,
        [property: JsonPropertyName("action_items")] ActionItem[] ActionItems,
        [property: JsonPropertyName("summary")] string Summary,
        [property: JsonPropertyName("next_meeting")] string? NextMeeting
    );
}

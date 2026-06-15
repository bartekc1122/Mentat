namespace Mentat.Infrastructure.Transcription
{
    public record TranscriptSegment(string Speaker, string Text, double Start, double End);

    public record DiarizedTranscript(IReadOnlyList<TranscriptSegment> Segments)
    {
        public bool IsEmpty => Segments.Count == 0;

        // Renders the diarized transcript as labelled lines, e.g. "Bartek: ..." or "Person 1: ...".
        public string ToText() =>
            string.Join("\n", Segments.Select(s => $"{s.Speaker}: {s.Text}"));
    }
}

namespace Mentat.Infrastructure.Transcription
{
    public record TranscriptSegment(string Speaker, string Text, double Start, double End);

    public record DiarizedTranscript(IReadOnlyList<TranscriptSegment> Segments)
    {
        public bool IsEmpty => Segments.Count == 0;

        // Renders the diarized transcript as labelled lines, e.g. "Speaker A: ...".
        public string ToText() =>
            string.Join("\n", Segments.Select(s => $"Speaker {s.Speaker}: {s.Text}"));
    }
}

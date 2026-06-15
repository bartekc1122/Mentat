using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mentat.Infrastructure.LLM.Client;
using Mentat.Infrastructure.Transcription;

namespace Mentat.Infrastructure.LLM
{
    // Maps anonymous diarization labels (A, B, C, ...) to real names when the
    // conversation reveals them, falling back to "Person 1", "Person 2", ... otherwise.
    public class SpeakerResolver
    {
        private const string SchemaName = "speaker_names";
        private const string Model = "gpt-5-mini";

        private const string SchemaJson = """
        {
            "type": "object",
            "properties": {
                "speakers": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "label": { "type": "string" },
                            "name": { "type": "string" }
                        },
                        "required": ["label", "name"],
                        "additionalProperties": false
                    }
                }
            },
            "required": ["speakers"],
            "additionalProperties": false
        }
        """;

        private const string SystemPrompt = """
        You analyze conversations. You receive a transcript with anonymous speaker labels (e.g. "A", "B", "C") and the text of what each speaker said.
        For each label, determine the person's actual name ONLY if it is unambiguously revealed by the conversation. Apply these rules carefully:

        - VOCATIVE RULE (important): a name used to address or call someone — e.g. a turn starting with "Bartek, ..." or using "Bartek" to get attention — identifies the LISTENER being spoken to, NOT the speaker who utters it. That name belongs to a DIFFERENT speaker, usually the one in the immediately preceding or following turn. Never assign such a name to the speaker who says it.
        - A speaker's OWN name comes from self-reference, e.g. "I'm Bartek", "tu Bartek", "mówi Bartek", or signing a message.
        - Do not guess based on role, tone, topic, or likelihood.

        If a person's name is not revealed, assign them "Person 1", "Person 2", "Person 3", and so on — numbering the unnamed people by the order in which their label first appears in the transcript.
        If a name is revealed at any point in the conversation, use it for that label across the whole transcript.
        Return a mapping of every label that appears in the transcript to its resolved name. Do not omit any label and do not invent labels that are not in the transcript.
        """;

        private readonly ILLMChatConnection _client;

        public SpeakerResolver(IConnectionProvider provider)
        {
            _client = provider.CreateConnection(SchemaName, BinaryData.FromString(SchemaJson), SystemPrompt, Model);
        }

        public async Task<DiarizedTranscript> ResolveAsync(DiarizedTranscript transcript, CancellationToken cancellationToken = default)
        {
            if (transcript.IsEmpty)
                return transcript;

            string input = BuildInput(transcript);
            string json = await _client.MakeCallAsync(input, cancellationToken);

            SpeakerMap? map = JsonSerializer.Deserialize<SpeakerMap>(json);
            Dictionary<string, string> names = map?.Speakers?
                .Where(s => !string.IsNullOrWhiteSpace(s.Label) && !string.IsNullOrWhiteSpace(s.Name))
                .ToDictionary(s => s.Label!, s => s.Name!)
                ?? new Dictionary<string, string>();

            var relabeled = transcript.Segments
                .Select(seg => names.TryGetValue(seg.Speaker, out string? name) ? seg with { Speaker = name } : seg)
                .ToList();

            return new DiarizedTranscript(relabeled);
        }

        // Feeds the model raw labelled lines, e.g. "A: ...", so the mapping keys line up with segment labels.
        private static string BuildInput(DiarizedTranscript transcript)
        {
            var sb = new StringBuilder();
            foreach (TranscriptSegment seg in transcript.Segments)
                sb.AppendLine($"{seg.Speaker}: {seg.Text}");
            return sb.ToString();
        }

        private sealed class SpeakerMap
        {
            [JsonPropertyName("speakers")]
            public List<SpeakerName>? Speakers { get; set; }
        }

        private sealed class SpeakerName
        {
            [JsonPropertyName("label")]
            public string? Label { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }
        }
    }
}

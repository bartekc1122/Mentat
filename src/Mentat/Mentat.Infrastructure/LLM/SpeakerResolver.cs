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
        private const string Model = LlmModels.Chat;

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
        Analizujesz rozmowy. Rozmowa jest po polsku, więc imiona osób są polskie.
        Otrzymujesz transkrypt z anonimowymi etykietami mówców (np. "A", "B", "C") oraz treść wypowiedzi każdego z nich.
        Dla każdej etykiety ustal prawdziwe imię osoby TYLKO wtedy, gdy jest jednoznacznie ujawnione w rozmowie. Stosuj uważnie poniższe zasady:

        - ZASADA WOŁACZA (ważne): imię użyte, by się do kogoś zwrócić lub go zawołać — np. wypowiedź zaczynająca się od "Bartek, ..." albo użycie "Bartek", by zwrócić czyjąś uwagę — wskazuje SŁUCHACZA, do którego się mówi, a NIE mówiącego, który je wypowiada. To imię należy do INNEGO mówcy, zwykle tego z bezpośrednio poprzedzającej lub następnej wypowiedzi. Nigdy nie przypisuj takiego imienia mówcy, który je wypowiada.
        - WŁASNE imię mówcy pochodzi z odniesienia do samego siebie, np. "jestem Bartek", "tu Bartek", "mówi Bartek" albo podpisu wiadomości.
        - Nie zgaduj na podstawie roli, tonu, tematu ani prawdopodobieństwa.

        Jeśli imię osoby nie zostało ujawnione, przypisz jej "Osoba 1", "Osoba 2", "Osoba 3" itd. — numerując nienazwane osoby według kolejności pierwszego pojawienia się ich etykiety w transkrypcie.
        Jeśli imię zostanie ujawnione w którymkolwiek momencie rozmowy, użyj go dla tej etykiety w całym transkrypcie.
        Zwróć mapowanie każdej etykiety występującej w transkrypcie na ustalone imię. Nie pomijaj żadnej etykiety i nie wymyślaj etykiet, których nie ma w transkrypcie.
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

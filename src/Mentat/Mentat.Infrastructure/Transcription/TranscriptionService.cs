using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mentat.Infrastructure.Transcription
{
    public class TranscriptionService : ITranscriptionService
    {
        private const string Model = "gpt-4o-transcribe-diarize";
        private const string Language = "pl";
        private const string Endpoint = "https://api.openai.com/v1/audio/transcriptions";

        private static readonly HttpClient _http = new();
        private readonly string _apiKey;

        public TranscriptionService(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<DiarizedTranscript> TranscribeAsync(Stream audio, string filename, CancellationToken cancellationToken = default)
        {
            using var form = new MultipartFormDataContent();

            var fileContent = new StreamContent(audio);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            form.Add(fileContent, "file", filename);
            form.Add(new StringContent(Model), "model");
            form.Add(new StringContent(Language), "language");
            form.Add(new StringContent("diarized_json"), "response_format");
            form.Add(new StringContent("auto"), "chunking_strategy");

            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = form };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);
            string body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Transcription failed ({(int)response.StatusCode}): {body}");

            DiarizedResponse? parsed = JsonSerializer.Deserialize<DiarizedResponse>(body);

            var segments = parsed?.Segments?
                .Select(s => new TranscriptSegment(s.Speaker ?? "?", s.Text ?? string.Empty, s.Start, s.End))
                .ToList() ?? new List<TranscriptSegment>();

            return new DiarizedTranscript(segments);
        }

        private sealed class DiarizedResponse
        {
            [JsonPropertyName("segments")]
            public List<Segment>? Segments { get; set; }
        }

        private sealed class Segment
        {
            [JsonPropertyName("speaker")]
            public string? Speaker { get; set; }

            [JsonPropertyName("text")]
            public string? Text { get; set; }

            [JsonPropertyName("start")]
            public double Start { get; set; }

            [JsonPropertyName("end")]
            public double End { get; set; }
        }
    }
}

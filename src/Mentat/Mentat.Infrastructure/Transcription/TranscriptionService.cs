using System.ClientModel;
using OpenAI.Audio;

namespace Mentat.Infrastructure.Transcription
{
    public class TranscriptionService : ITranscriptionService
    {
        private const string model = "whisper-1";
        private readonly AudioClient _client;

        public TranscriptionService(string apiKey)
        {
            _client = new AudioClient(model, new ApiKeyCredential(apiKey));
        }

        public async Task<string> TranscribeAsync(Stream audio, string filename, CancellationToken cancellationToken = default)
        {
            AudioTranscription transcription = await _client.TranscribeAudioAsync(
                audio,
                filename,
                cancellationToken: cancellationToken);

            return transcription.Text;
        }
    }
}

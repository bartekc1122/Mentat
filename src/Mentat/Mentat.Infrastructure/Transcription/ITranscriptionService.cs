namespace Mentat.Infrastructure.Transcription
{
    public interface ITranscriptionService
    {
        Task<string> TranscribeAsync(Stream audio, string filename, CancellationToken cancellationToken = default);
    }
}

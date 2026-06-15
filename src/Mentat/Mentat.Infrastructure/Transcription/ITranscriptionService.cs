namespace Mentat.Infrastructure.Transcription
{
    public interface ITranscriptionService
    {
        Task<DiarizedTranscript> TranscribeAsync(Stream audio, string filename, CancellationToken cancellationToken = default);
    }
}

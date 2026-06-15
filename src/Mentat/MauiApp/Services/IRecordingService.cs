namespace Mentat.Services
{
    public interface IRecordingService
    {
        bool IsRecording { get; }

        Task StartAsync(CancellationToken cancellationToken = default);

        Task<Stream> StopAsync();
    }
}

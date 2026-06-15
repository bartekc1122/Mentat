using Plugin.Maui.Audio;

namespace Mentat.Services
{
    public class RecordingService : IRecordingService
    {
        private readonly IAudioManager _audioManager;
        private IAudioRecorder? _recorder;

        public RecordingService(IAudioManager audioManager)
        {
            _audioManager = audioManager;
        }

        public bool IsRecording => _recorder?.IsRecording ?? false;

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (IsRecording)
                return;

            var status = await Permissions.RequestAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
                throw new PermissionException("Microphone permission was not granted.");

            _recorder = _audioManager.CreateRecorder();
            await _recorder.StartAsync();
        }

        public async Task<Stream> StopAsync()
        {
            if (_recorder is null || !_recorder.IsRecording)
                throw new InvalidOperationException("Recording has not been started.");

            IAudioSource source = await _recorder.StopAsync();
            _recorder = null;

            return source.GetAudioStream();
        }
    }
}

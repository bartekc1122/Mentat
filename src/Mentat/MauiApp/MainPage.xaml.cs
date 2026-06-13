using Mentat.Infrastructure.Transcription;
using Mentat.Services;

namespace Mentat;

public partial class MainPage : ContentPage
{
    private enum State { Idle, Recording, Processing }

    private readonly IRecordingService _recording;
    private readonly ITranscriptionService _transcription;

    private State _state = State.Idle;

    public MainPage(IRecordingService recording, ITranscriptionService transcription)
    {
        InitializeComponent();
        _recording = recording;
        _transcription = transcription;
    }

    private async void OnRecordClicked(object? sender, EventArgs e)
    {
        switch (_state)
        {
            case State.Idle:
                await StartRecordingAsync();
                break;
            case State.Recording:
                await StopAndTranscribeAsync();
                break;
            case State.Processing:
                // Ignored — button is disabled while processing.
                break;
        }
    }

    private async Task StartRecordingAsync()
    {
        try
        {
            await _recording.StartAsync();

            _state = State.Recording;
            RecordBtn.Text = "Stop";
            StatusLabel.Text = "Recording...";
            TranscriptLabel.Text = "";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Could not start recording: {ex.Message}", "OK");
            ResetToIdle();
        }
    }

    private async Task StopAndTranscribeAsync()
    {
        SetProcessing("Transcribing...");

        try
        {
            Stream audio = await _recording.StopAsync();
            string transcript = await _transcription.TranscribeAsync(audio, "recording.wav");

            TranscriptLabel.Text = transcript;
            StatusLabel.Text = "Done";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Transcription failed: {ex.Message}", "OK");
            StatusLabel.Text = "";
        }
        finally
        {
            ResetToIdle();
        }
    }

    private void SetProcessing(string status)
    {
        _state = State.Processing;
        RecordBtn.IsEnabled = false;
        BusyIndicator.IsRunning = true;
        BusyIndicator.IsVisible = true;
        StatusLabel.Text = status;
    }

    private void ResetToIdle()
    {
        _state = State.Idle;
        RecordBtn.IsEnabled = true;
        RecordBtn.Text = "Record";
        BusyIndicator.IsRunning = false;
        BusyIndicator.IsVisible = false;
    }
}

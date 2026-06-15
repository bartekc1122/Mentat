using Mentat.Infrastructure.LLM;
using Mentat.Infrastructure.Pipeline;
using Mentat.Infrastructure.Storage;
using Mentat.Infrastructure.Transcription;
using Mentat.Services;

namespace Mentat;

public partial class MainPage : ContentPage
{
    private enum State { Idle, Recording, Processing }

    private readonly IRecordingService _recording;
    private readonly ITranscriptionService _transcription;
    private readonly SpeakerResolver _speakerResolver;
    private readonly MeetingProcessor _processor;
    private readonly MeetingDatabase _database;

    private State _state = State.Idle;
    private int? _lastMeetingId;

    public MainPage(
        IRecordingService recording,
        ITranscriptionService transcription,
        SpeakerResolver speakerResolver,
        MeetingProcessor processor,
        MeetingDatabase database)
    {
        InitializeComponent();
        _recording = recording;
        _transcription = transcription;
        _speakerResolver = speakerResolver;
        _processor = processor;
        _database = database;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadProjectsAsync();
    }

    private async Task LoadProjectsAsync()
    {
        int? selectedId = (ProjectPicker.SelectedItem as Project)?.Id;

        List<Project> projects = await _database.GetProjectsAsync();
        if (projects.Count == 0)
        {
            await _database.GetOrCreateDefaultProjectAsync();
            projects = await _database.GetProjectsAsync();
        }

        ProjectPicker.ItemsSource = projects;

        int index = selectedId is int id ? projects.FindIndex(p => p.Id == id) : 0;
        ProjectPicker.SelectedIndex = index >= 0 ? index : 0;
    }

    private async void OnRecordClicked(object? sender, EventArgs e)
    {
        switch (_state)
        {
            case State.Idle:
                await StartRecordingAsync();
                break;
            case State.Recording:
                await StopTranscribeAndProcessAsync();
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
            ViewNotesBtn.IsVisible = false;
            _lastMeetingId = null;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Could not start recording: {ex.Message}", "OK");
            ResetToIdle();
        }
    }

    private async Task StopTranscribeAndProcessAsync()
    {
        SetProcessing("Transcribing...");

        try
        {
            Stream audio = await _recording.StopAsync();
            DiarizedTranscript transcript = await _transcription.TranscribeAsync(audio, "recording.wav");

            StatusLabel.Text = "Identifying speakers...";
            DiarizedTranscript named = await _speakerResolver.ResolveAsync(transcript);

            if (named.IsEmpty)
            {
                TranscriptLabel.Text = "(no speech detected)";
                StatusLabel.Text = "Done";
                return;
            }

            TranscriptLabel.Text = named.ToText();

            // Wpięcie pipeline'u: ekstrakcja notatek + zapis w bazie pod wybranym projektem.
            StatusLabel.Text = "Analizuję notatki...";
            Project project = (ProjectPicker.SelectedItem as Project) ?? await _database.GetOrCreateDefaultProjectAsync();

            ProcessResult result = await _processor.ProcessAsync(named.ToText(), project.Id);

            _lastMeetingId = result.Meeting.Id;
            StatusLabel.Text = $"Zapisano w „{project.Name}”: {result.NoteCount} notatek";
            ViewNotesBtn.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Przetwarzanie nie powiodło się: {ex.Message}", "OK");
            StatusLabel.Text = "";
        }
        finally
        {
            ResetToIdle();
        }
    }

    private async void OnViewNotesClicked(object? sender, EventArgs e)
    {
        if (_lastMeetingId is int meetingId)
            await Shell.Current.GoToAsync($"meetingDetail?meetingId={meetingId}");
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

using Mentat.Infrastructure.Storage;

namespace Mentat;

[QueryProperty(nameof(ProjectId), "projectId")]
[QueryProperty(nameof(ProjectName), "projectName")]
public partial class ProjectDetailPage : ContentPage
{
    private readonly MeetingDatabase _database;

    public ProjectDetailPage(MeetingDatabase database)
    {
        InitializeComponent();
        _database = database;
    }

    public string ProjectId { get; set; } = "";

    public string ProjectName
    {
        set => Title = Uri.UnescapeDataString(value);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (int.TryParse(ProjectId, out int projectId))
            MeetingsList.ItemsSource = await _database.GetMeetingsByProjectAsync(projectId);
    }

    private async void OnMeetingSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Meeting meeting)
            return;

        MeetingsList.SelectedItem = null;
        await Shell.Current.GoToAsync($"meetingDetail?meetingId={meeting.Id}");
    }
}

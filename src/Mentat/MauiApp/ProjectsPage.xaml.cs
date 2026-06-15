using Mentat.Infrastructure.Storage;

namespace Mentat;

public partial class ProjectsPage : ContentPage
{
    private readonly MeetingDatabase _database;

    public ProjectsPage(MeetingDatabase database)
    {
        InitializeComponent();
        _database = database;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        ProjectsList.ItemsSource = await _database.GetProjectsAsync();
    }

    private async void OnAddProjectClicked(object? sender, EventArgs e)
    {
        string name = NewProjectEntry.Text?.Trim() ?? "";
        if (name.Length == 0)
            return;

        await _database.AddProjectAsync(new Project { Name = name, CreatedAt = DateTime.UtcNow });
        NewProjectEntry.Text = "";
        await ReloadAsync();
    }

    private async void OnProjectSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Project project)
            return;

        ProjectsList.SelectedItem = null; // pozwala wybrać ten sam projekt ponownie po powrocie

        await Shell.Current.GoToAsync($"projectDetail?projectId={project.Id}&projectName={Uri.EscapeDataString(project.Name)}");
    }
}

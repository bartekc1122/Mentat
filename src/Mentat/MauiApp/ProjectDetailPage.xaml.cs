using Mentat.Infrastructure.Search;
using Mentat.Infrastructure.Storage;

namespace Mentat;

[QueryProperty(nameof(ProjectId), "projectId")]
[QueryProperty(nameof(ProjectName), "projectName")]
public partial class ProjectDetailPage : ContentPage
{
    private readonly MeetingDatabase _database;
    private readonly SemanticSearchService _search;

    private int _projectId;

    public ProjectDetailPage(MeetingDatabase database, SemanticSearchService search)
    {
        InitializeComponent();
        _database = database;
        _search = search;
    }

    public string ProjectId { get; set; } = "";

    public string ProjectName
    {
        set => Title = Uri.UnescapeDataString(value);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (int.TryParse(ProjectId, out _projectId))
            MeetingsList.ItemsSource = await _database.GetMeetingsByProjectAsync(_projectId);
    }

    private async void OnSearch(object? sender, EventArgs e)
    {
        string query = SearchBox.Text?.Trim() ?? "";
        if (query.Length == 0)
        {
            ShowMeetings();
            return;
        }

        ResultsEmptyLabel.Text = "Szukam...";
        ResultsList.ItemsSource = null;
        MeetingsList.IsVisible = false;
        ResultsList.IsVisible = true;

        IReadOnlyList<SearchResult> results = await _search.SearchAsync(query, _projectId, topK: 15);

        Dictionary<int, string> titles = (await _database.GetMeetingsByProjectAsync(_projectId))
            .ToDictionary(m => m.Id, m => m.Title);

        ResultsEmptyLabel.Text = "Brak wyników.";
        ResultsList.ItemsSource = results
            .Select(r => new SearchResultView(r, titles.GetValueOrDefault(r.MeetingId, "Spotkanie")))
            .ToList();
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        // Wyczyszczenie pola wraca do listy spotkań.
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
            ShowMeetings();
    }

    private void ShowMeetings()
    {
        ResultsList.IsVisible = false;
        ResultsList.ItemsSource = null;
        MeetingsList.IsVisible = true;
    }

    private async void OnMeetingSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Meeting meeting)
            return;

        MeetingsList.SelectedItem = null;
        await Shell.Current.GoToAsync($"meetingDetail?meetingId={meeting.Id}");
    }

    private async void OnResultSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not SearchResultView result)
            return;

        ResultsList.SelectedItem = null;
        await Shell.Current.GoToAsync($"meetingDetail?meetingId={result.MeetingId}");
    }
}

/// <summary>Widok pojedynczego wyniku wyszukiwania (element + spotkanie, z którego pochodzi).</summary>
public sealed class SearchResultView
{
    public SearchResultView(SearchResult result, string meetingTitle)
    {
        MeetingId = result.MeetingId;
        Headline = result.Content;
        Detail = result.Quote;
        Meta = $"{KindLabel(result.Kind)} • {meetingTitle}";
    }

    public int MeetingId { get; }
    public string Headline { get; }
    public string Detail { get; }
    public bool HasDetail => !string.IsNullOrWhiteSpace(Detail);
    public string Meta { get; }

    private static string KindLabel(string kind) => kind switch
    {
        ItemKinds.Informacja => "Informacja",
        ItemKinds.Zadanie => "Zadanie",
        _ => "Element",
    };
}

using System.Text.Json;
using Mentat.Infrastructure.Pipeline;
using Mentat.Infrastructure.Storage;

namespace Mentat;

[QueryProperty(nameof(MeetingId), "meetingId")]
public partial class MeetingDetailPage : ContentPage
{
    private readonly MeetingDatabase _database;
    private bool _loaded;

    public MeetingDetailPage(MeetingDatabase database)
    {
        InitializeComponent();
        _database = database;
    }

    public string MeetingId { get; set; } = "";

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded || !int.TryParse(MeetingId, out int meetingId))
            return;

        _loaded = true;
        await LoadAsync(meetingId);
    }

    private async Task LoadAsync(int meetingId)
    {
        Meeting? meeting = await _database.GetMeetingAsync(meetingId);
        if (meeting is null)
        {
            Container.Add(new Label { Text = "Nie znaleziono spotkania.", TextColor = Colors.Gray });
            return;
        }

        Title = meeting.Title;
        Dictionary<string, string> quotes = BuildQuoteLookup(meeting.UtterancesJson);
        List<Note> notes = await _database.GetNotesByMeetingAsync(meetingId);

        AddSection("Tematy", notes.Where(n => n.Type == NoteTypes.Topic), quotes);
        AddSection("Decyzje", notes.Where(n => n.Type == NoteTypes.Decision), quotes);
        AddSection("Zadania", notes.Where(n => n.Type == NoteTypes.Action), quotes);

        if (Container.Count == 0)
            Container.Add(new Label { Text = "Brak notatek dla tego spotkania.", TextColor = Colors.Gray });
    }

    private void AddSection(string header, IEnumerable<Note> notes, Dictionary<string, string> quotes)
    {
        var list = notes.ToList();
        if (list.Count == 0)
            return;

        Container.Add(new Label
        {
            Text = header,
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 8, 0, 0),
        });

        foreach (Note note in list)
            Container.Add(BuildNoteCard(note, quotes));
    }

    private static Border BuildNoteCard(Note note, Dictionary<string, string> quotes)
    {
        var content = new VerticalStackLayout { Spacing = 6 };

        if (!string.IsNullOrWhiteSpace(note.Title))
            content.Add(new Label { Text = note.Title, FontAttributes = FontAttributes.Bold, FontSize = 16 });

        if (!string.IsNullOrWhiteSpace(note.Body))
            content.Add(new Label { Text = note.Body, FontSize = 15 });

        string meta = BuildActionMeta(note);
        if (meta.Length > 0)
            content.Add(new Label { Text = meta, FontSize = 13, TextColor = Colors.DarkSlateGray });

        // Linki/cytaty: rozwijamy refy źródłowych wypowiedzi na treść.
        foreach (string reference in note.SourceRefs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string quote = quotes.TryGetValue(reference, out string? text) ? text : reference;
            content.Add(new Label
            {
                Text = $"„{quote}”",
                FontSize = 13,
                FontAttributes = FontAttributes.Italic,
                TextColor = Colors.Gray,
                Margin = new Thickness(8, 0, 0, 0),
            });
        }

        return new Border
        {
            Padding = 12,
            StrokeThickness = 1,
            Stroke = Colors.LightGray,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Content = content,
        };
    }

    private static string BuildActionMeta(Note note)
    {
        if (note.Type != NoteTypes.Action)
            return "";

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(note.Owner)) parts.Add($"Właściciel: {note.Owner}");
        if (!string.IsNullOrWhiteSpace(note.Deadline)) parts.Add($"Termin: {note.Deadline}");
        if (!string.IsNullOrWhiteSpace(note.Blocker)) parts.Add($"Bloker: {note.Blocker}");
        return string.Join("  •  ", parts);
    }

    private static Dictionary<string, string> BuildQuoteLookup(string utterancesJson)
    {
        var lookup = new Dictionary<string, string>();
        try
        {
            var utterances = JsonSerializer.Deserialize<List<Utterance>>(utterancesJson) ?? [];
            foreach (Utterance u in utterances)
                lookup[u.Ref] = $"{u.Speaker}: {u.Text}";
        }
        catch
        {
            // Uszkodzony JSON — pokażemy same identyfikatory refów.
        }
        return lookup;
    }
}

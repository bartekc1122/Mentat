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

        // Timestamp — kiedy spotkanie się odbyło.
        Container.Add(new Label
        {
            Text = $"Spotkanie z {meeting.CreatedAt:yyyy-MM-dd HH:mm}",
            FontSize = 13,
            TextColor = Colors.Gray,
        });

        List<Note> notes = await _database.GetNotesByMeetingAsync(meetingId);

        AddSection("Informacje", notes.Where(n => n.Kind == ItemKinds.Informacja));
        AddSection("Zadania", notes.Where(n => n.Kind == ItemKinds.Zadanie));

        if (notes.Count == 0)
            Container.Add(new Label { Text = "Brak elementów dla tego spotkania.", TextColor = Colors.Gray });
    }

    private void AddSection(string header, IEnumerable<Note> notes)
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
            Container.Add(BuildNoteCard(note));
    }

    private static Border BuildNoteCard(Note note)
    {
        var content = new VerticalStackLayout { Spacing = 6 };

        content.Add(new Label { Text = note.Content, FontSize = 16, FontAttributes = FontAttributes.Bold });

        string meta = BuildTaskMeta(note);
        if (meta.Length > 0)
            content.Add(new Label { Text = meta, FontSize = 13, TextColor = Colors.DarkSlateGray });

        // Dosłowny cytat z transkryptu, w którym element padł.
        if (!string.IsNullOrWhiteSpace(note.Quote))
            content.Add(new Label
            {
                Text = $"„{note.Quote}”",
                FontSize = 13,
                FontAttributes = FontAttributes.Italic,
                TextColor = Colors.Gray,
                Margin = new Thickness(8, 0, 0, 0),
            });

        return new Border
        {
            Padding = 12,
            StrokeThickness = 1,
            Stroke = Colors.LightGray,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Content = content,
        };
    }

    private static string BuildTaskMeta(Note note)
    {
        if (note.Kind != ItemKinds.Zadanie)
            return "";

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(note.Owner)) parts.Add($"Właściciel: {note.Owner}");
        if (!string.IsNullOrWhiteSpace(note.Deadline)) parts.Add($"Termin: {note.Deadline}");
        return string.Join("  •  ", parts);
    }
}

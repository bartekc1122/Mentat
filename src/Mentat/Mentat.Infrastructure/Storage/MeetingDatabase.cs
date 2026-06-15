using SQLite;

namespace Mentat.Infrastructure.Storage;

/// <summary>
/// Owijka na SQLite (plik mentat.db3). Leniwie tworzy tabele przy pierwszym użyciu.
/// </summary>
public sealed class MeetingDatabase
{
    private readonly SQLiteAsyncConnection _connection;
    private bool _initialized;

    public MeetingDatabase(string databasePath)
    {
        _connection = new SQLiteAsyncConnection(databasePath);
    }

    private async Task InitAsync()
    {
        if (_initialized)
            return;

        await _connection.CreateTableAsync<Meeting>();
        await _connection.CreateTableAsync<Note>();
        _initialized = true;
    }

    /// <summary>Zamyka połączenie i zwalnia plik bazy (przydatne w testach / przy przełączaniu bazy).</summary>
    public Task CloseAsync() => _connection.CloseAsync();

    public async Task<int> AddMeetingAsync(Meeting meeting)
    {
        await InitAsync();
        await _connection.InsertAsync(meeting);
        return meeting.Id; // uzupełniane przez AutoIncrement
    }

    public async Task AddNotesAsync(IEnumerable<Note> notes)
    {
        await InitAsync();
        await _connection.InsertAllAsync(notes);
    }

    public async Task<Meeting?> GetMeetingAsync(int id)
    {
        await InitAsync();
        return await _connection.FindAsync<Meeting>(id);
    }

    public async Task<List<Note>> GetNotesByMeetingAsync(int meetingId)
    {
        await InitAsync();
        return await _connection.Table<Note>().Where(n => n.MeetingId == meetingId).ToListAsync();
    }

    /// <summary>Notatki danego projektu (lub wszystkie, gdy projectId == null) — baza wyszukiwania.</summary>
    public async Task<List<Note>> GetNotesByProjectAsync(int? projectId)
    {
        await InitAsync();
        return projectId is int pid
            ? await _connection.Table<Note>().Where(n => n.ProjectId == pid).ToListAsync()
            : await _connection.Table<Note>().ToListAsync();
    }
}

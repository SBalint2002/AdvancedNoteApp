using AdvancedNoteApp.Models;
using SQLite;

namespace AdvancedNoteApp.Services;

public class LocalDatabase : ILocalDatabase
{
    SQLiteAsyncConnection? database;

    async Task Init()
    {
        if (database is not null) return;
        database = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
        await database.CreateTableAsync<Note>();
    }

    public async Task<List<Note>> GetNotesAsync()
    {
        return (await GetAllNotesAsync()).Where(n => !n.Deleted).ToList();
    }

    public async Task<List<Note>> GetAllNotesAsync()
    {
        await Init();
        return await database!.Table<Note>().OrderByDescending(n => n.UpdatedAt).ToListAsync();
    }

    public async Task InsertNoteAsync(Note note)
    {
        await Init();
        note.CreatedAt = note.CreatedAt == default ? DateTime.UtcNow : note.CreatedAt;
        note.UpdatedAt = DateTime.UtcNow;
        note.Synced = false;
        await database!.InsertAsync(note);
    }

    public async Task UpdateNoteAsync(Note note, bool synced = false)
    {
        await Init();
        note.UpdatedAt = DateTime.UtcNow;
        note.Synced = synced;
        await database!.UpdateAsync(note);
    }

    public async Task MarkDeletedAsync(Note note)
    {
        await Init();
        note.Deleted = true;
        note.Synced = false;
        note.UpdatedAt = DateTime.UtcNow;
        await database!.UpdateAsync(note);
    }

    public async Task RemoveNoteAsync(Note note)
    {
        await Init();
        await database!.DeleteAsync(note);
    }
}
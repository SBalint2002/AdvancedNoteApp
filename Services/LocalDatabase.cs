using AdvancedNoteApp.Models;
using SQLite;

namespace AdvancedNoteApp.Services;

public class LocalDatabase : ILocalDatabase
{
    SQLiteAsyncConnection? database;

    async Task Init()
    {
        if (database is not null)
            return;
        database = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
        var result = await database.CreateTableAsync<Note>();
    }

    public async Task<List<Note>> GetNotesAsync()
    {
        await Init();
        return await database!.Table<Note>()
            .Where(n => !n.Deleted)
            .OrderByDescending(n => n.UpdatedAt)
            .ToListAsync();
    }

    public async Task SaveNoteAsync(Note note)
    {
        await Init();
        if (note is null) throw new ArgumentNullException(nameof(note));

        if (note.Id != 0)
        {
            note.UpdatedAt = DateTime.UtcNow;
            await database!.UpdateAsync(note);
        }
        else
        {
            note.CreatedAt = DateTime.UtcNow;
            note.UpdatedAt = DateTime.UtcNow;
            await database!.InsertAsync(note);
        }
    }

    public async Task DeleteNoteAsync(Note note)
    {
        await Init();
        if (note is null) throw new ArgumentNullException(nameof(note));
        note.Deleted = true;
        note.UpdatedAt = DateTime.UtcNow;
        await database!.UpdateAsync(note);
    }
}
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

    public async Task<List<Note>> GetAllNotesAsync()
    {
        await Init();
        return await database!.Table<Note>()
            .OrderByDescending(n => n.UpdatedAt)
            .ToListAsync();
    }

    public async Task SaveNoteAsync(Note note)
    {
        await Init();
        if (note is null) throw new ArgumentNullException(nameof(note));

        note.Synced = false;
        note.UpdatedAt = DateTime.UtcNow;

        var exists = await database!
            .Table<Note>()
            .Where(n => n.Id == note.Id)
            .CountAsync();

        if (exists > 0)
        {
            await database!.UpdateAsync(note);
        }
        else
        {
            note.CreatedAt = DateTime.UtcNow;
            await database!.InsertAsync(note);
        }
    }

    public async Task UpsertNoteAsync(Note note)
    {
        await Init();
        if (note is null) throw new ArgumentNullException(nameof(note));

        await database!.RunInTransactionAsync(conn =>
        {
            var rows = conn.Update(note);
            if (rows == 0)
            {
                conn.Insert(note);
            }
        });
    }

    public async Task DeleteNoteAsync(Note note)
    {
        await Init();
        if (note is null) throw new ArgumentNullException(nameof(note));
        note.Deleted = true;
        note.Synced = false;
        note.UpdatedAt = DateTime.UtcNow;
        await database!.UpdateAsync(note);
    }

    public async Task RemoveNoteAsync(Note note)
    {
        await Init();
        if (note is null) throw new ArgumentNullException(nameof(note));
        await database!.DeleteAsync(note);
    }
}
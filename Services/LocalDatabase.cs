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

    public async Task SaveNoteAsync(Note note, bool markAsSynced = false)
    {
        await Init();
        if (note is null) throw new ArgumentNullException(nameof(note));

        var exists = await database!
            .Table<Note>()
            .Where(n => n.Id == note.Id)
            .CountAsync();

        if (exists > 0)
        {
            var current = await database!.Table<Note>()
                .Where(n => n.Id == note.Id)
                .FirstOrDefaultAsync();

            if (current is not null)
            {
                bool contentEqual = current.Title == note.Title
                                    && current.Content == note.Content
                                    && current.ImageUrl == note.ImageUrl;

                if (contentEqual && !markAsSynced)
                {
                    return;
                }

                if (contentEqual && markAsSynced)
                {
                    if (current.Synced) return;
                    note.Synced = true;
                    note.CreatedAt = current.CreatedAt;
                    note.UpdatedAt = current.UpdatedAt;
                    await database.UpdateAsync(note);
                    return;
                }

                if (!markAsSynced)
                {
                    note.Synced = false;
                    note.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    note.Synced = true;
                }

                note.CreatedAt = current.CreatedAt;
                await database.UpdateAsync(note);
            }

            return;
        }

        if (!markAsSynced)
        {
            note.Synced = false;
            note.CreatedAt = DateTime.UtcNow;
            note.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            note.Synced = true;
            if (note.CreatedAt == default) note.CreatedAt = DateTime.UtcNow;
            if (note.UpdatedAt == default) note.UpdatedAt = DateTime.UtcNow;
        }

        await database!.InsertAsync(note);
    }

    public async Task UpsertNoteAsync(Note note, bool markAsSynced = false)
    {
        await Init();
        if (note is null) throw new ArgumentNullException(nameof(note));

        var now = DateTime.UtcNow;

        if (note.Id != 0)
        {
            var existing = await database!.Table<Note>()
                                       .Where(n => n.Id == note.Id)
                                       .FirstOrDefaultAsync();

            if (existing is not null)
            {
                bool contentEqual = existing.Title == note.Title
                                    && existing.Content == note.Content
                                    && existing.ImageUrl == note.ImageUrl;

                if (contentEqual && !markAsSynced)
                {
                    return;
                }

                note.CreatedAt = existing.CreatedAt;

                if (!markAsSynced)
                {
                    note.Synced = false;
                    note.UpdatedAt = now;
                }
                else
                {
                    note.Synced = true;
                    if (note.UpdatedAt == default) note.UpdatedAt = existing.UpdatedAt;
                }

                await database.UpdateAsync(note);
                return;
            }
        }

        if (!markAsSynced)
        {
            note.Synced = false;
            note.UpdatedAt = now;
            note.CreatedAt = now;
        }
        else
        {
            note.Synced = true;
            if (note.CreatedAt == default) note.CreatedAt = now;
            if (note.UpdatedAt == default) note.UpdatedAt = now;
        }

        await database!.InsertAsync(note);
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
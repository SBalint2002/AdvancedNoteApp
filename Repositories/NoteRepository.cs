using AdvancedNoteApp.Models;
using AdvancedNoteApp.Services;

namespace AdvancedNoteApp.Repositories;

public class NoteRepository
{
    private readonly ILocalDatabase localDb;
    private readonly ISyncService syncService;

    public NoteRepository(ILocalDatabase localDb, ISyncService syncService)
    {
        this.localDb = localDb;
        this.syncService = syncService;
    }

    public Task<List<Note>> GetNotesAsync() => localDb.GetNotesAsync();

    public async Task SaveNoteAsync(Note note)
    {
        if (note.Id == 0)
        {
            await localDb.InsertNoteAsync(note);
        }
        else
        {
            await localDb.UpdateNoteAsync(note, false);
        }
    }

    public Task DeleteNoteAsync(Note note) => localDb.MarkDeletedAsync(note);

    public Task SyncNotesAsync() => syncService.SyncAllNotesAsync();
}
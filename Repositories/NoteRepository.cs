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
    public Task SaveNoteAsync(Note note) => localDb.SaveNoteAsync(note);
    public Task DeleteNoteAsync(Note note) => localDb.DeleteNoteAsync(note);
    public Task SyncNotesAsync() => syncService.SyncAllNotesAsync();
}
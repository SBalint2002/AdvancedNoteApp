using AdvancedNoteApp.Models;

namespace AdvancedNoteApp.Services;

public class LocalDatabase : ILocalDatabase
{
    public LocalDatabase ()
    {

    }

    public Task DeleteNoteAsync(Note note)
    {
        throw new NotImplementedException();
    }

    public Task<List<Note>> GetNotesAsync()
    {
        throw new NotImplementedException();
    }

    public Task SaveNoteAsync(Note note)
    {
        throw new NotImplementedException();
    }
}
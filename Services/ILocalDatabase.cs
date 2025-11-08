using AdvancedNoteApp.Models;

namespace AdvancedNoteApp.Services
{
    public interface ILocalDatabase
    {
        Task<List<Note>> GetNotesAsync();
        Task<List<Note>> GetAllNotesAsync();
        Task SaveNoteAsync(Note note, bool markAsSynced = false);
        Task UpsertNoteAsync(Note note, bool markAsSynced = true);
        Task DeleteNoteAsync(Note note);
        Task RemoveNoteAsync(Note note);
    }
}

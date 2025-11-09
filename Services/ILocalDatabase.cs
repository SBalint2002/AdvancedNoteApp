using AdvancedNoteApp.Models;

namespace AdvancedNoteApp.Services
{
    public interface ILocalDatabase
    {
        Task<List<Note>> GetNotesAsync();
        Task<List<Note>> GetAllNotesAsync();
        Task InsertNoteAsync(Note note);
        Task UpdateNoteAsync(Note note, bool synced);
        Task MarkDeletedAsync(Note note);
        Task RemoveNoteAsync(Note note);
    }
}

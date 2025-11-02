using AdvancedNoteApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedNoteApp.Services
{
    public interface ILocalDatabase
    {
        Task<List<Note>> GetNotesAsync();
        Task<List<Note>> GetAllNotesAsync();
        Task SaveNoteAsync(Note note);
        Task UpsertNoteAsync(Note note);
        Task DeleteNoteAsync(Note note);
        Task RemoveNoteAsync(Note note);
    }
}

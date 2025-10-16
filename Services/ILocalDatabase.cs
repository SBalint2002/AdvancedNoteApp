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
        Task SaveNoteAsync(Note note);
        Task DeleteNoteAsync(Note note);
    }
}

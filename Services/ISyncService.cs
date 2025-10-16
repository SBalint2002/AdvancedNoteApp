using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedNoteApp.Services
{
    public interface ISyncService
    {
        Task SyncAllNotesAsync();
    }
}

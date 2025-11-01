using AdvancedNoteApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedNoteApp.Messages
{
    public record NoteSavedMessage(Note Note);
}

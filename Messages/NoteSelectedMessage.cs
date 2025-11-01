using AdvancedNoteApp.Models;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AdvancedNoteApp.Messages;

public class NoteSelectedMessage : ValueChangedMessage<Note>
{
    public NoteSelectedMessage(Note note) : base(note) { }
}
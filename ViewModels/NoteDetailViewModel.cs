using AdvancedNoteApp.Models;
using AdvancedNoteApp.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AdvancedNoteApp.Messages;

namespace AdvancedNoteApp.ViewModels;

[QueryProperty(nameof(Note), "Note")]
public partial class NoteDetailViewModel : ObservableObject
{
    private readonly NoteRepository noteRepository;

    [ObservableProperty]
    private Note note = new Note();

    public NoteDetailViewModel(NoteRepository noteRepository)
    {
        this.noteRepository = noteRepository;

        WeakReferenceMessenger.Default.Register<NoteSelectedMessage>(this, (r, m) =>
        {
            Note = m.Value;
        });
    }

    [RelayCommand]
    public async Task SaveNoteAsync()
    {
        if (Note == null) return;

        await noteRepository.SaveNoteAsync(Note);
        WeakReferenceMessenger.Default.Send(new NoteSavedMessage(Note));
    }
}

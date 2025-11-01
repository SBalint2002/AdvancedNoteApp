using AdvancedNoteApp.Models;
using AdvancedNoteApp.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using AdvancedNoteApp.Messages;

namespace AdvancedNoteApp.ViewModels;

public partial class NotesListViewModel : ObservableObject
{
    private readonly NoteRepository noteRepository;

    [ObservableProperty]
    private ObservableCollection<Note> notes = new();

    public NotesListViewModel(NoteRepository noteRepository)
    {
        this.noteRepository = noteRepository;

        WeakReferenceMessenger.Default.Register<NoteSavedMessage>(this, async (r, m) =>
        {
            await LoadNotesAsync();
        });
    }

    [RelayCommand]
    public async Task LoadNotesAsync()
    {
        var allNotes = await noteRepository.GetNotesAsync();
        Notes = new ObservableCollection<Note>(allNotes);
    }

    [RelayCommand]
    public async Task AddNewNoteAsync()
    {
        await Shell.Current.GoToAsync("notedetail");
    }

    [RelayCommand]
    public async Task DeleteNoteAsync(Note note)
    {
        await noteRepository.DeleteNoteAsync(note);
        await LoadNotesAsync();
    }

    [RelayCommand]
    public async Task OpenNoteAsync(Note note)
    {
        if (note == null) return;

        await Shell.Current.GoToAsync("notedetail");
        WeakReferenceMessenger.Default.Send(new NoteSelectedMessage(note));
    }
}
using AdvancedNoteApp.Models;
using AdvancedNoteApp.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using AdvancedNoteApp.Messages;
using System.ComponentModel;

#pragma warning disable MVVMTK0045

namespace AdvancedNoteApp.ViewModels;

public partial class NotesListViewModel : ObservableObject
{
    private readonly NoteRepository noteRepository;

    [ObservableProperty]
    private ObservableCollection<Note> notes = new();

    [ObservableProperty]
    private Note? selectedNote = null;

    [ObservableProperty]
    private bool isSelectionMode = false;

    [ObservableProperty]
    private string selectionToggleText = "Kijelölés";

    [ObservableProperty]
    private bool hasSelectedItems = false;

    public NotesListViewModel(NoteRepository noteRepository)
    {
        this.noteRepository = noteRepository;

        WeakReferenceMessenger.Default.Register<NoteSavedMessage>(this, async (r, m) =>
        {
            await LoadNotesAsync();
        });
    }

    partial void OnIsSelectionModeChanged(bool value)
    {
        SelectionToggleText = value ? "Mégsem" : "Kijelölés";
        if (!value)
        {
            foreach (var n in Notes)
                n.IsSelected = false;
            HasSelectedItems = false;
            SelectedNote = null;
        }
    }

    [RelayCommand]
    public void ToggleSelectionMode()
    {
        IsSelectionMode = !IsSelectionMode;
    }

    [RelayCommand]
    public async Task TapItemAsync(Note note)
    {
        if (note == null) return;

        if (IsSelectionMode)
        {
            SelectItem(note);
            return;
        }

        await OpenNoteAsync(note);
    }

    public void SelectItem(Note note)
    {
        if (note == null) return;

        note.IsSelected = !note.IsSelected;
        HasSelectedItems = Notes.Any(n => n.IsSelected);

        SelectedNote = Notes.FirstOrDefault(n => n.IsSelected);
    }

    [RelayCommand]
    public async Task LoadNotesAsync()
    {
        if (Notes is not null)
        {
            foreach (var n in Notes)
                n.PropertyChanged -= Note_PropertyChanged;
        }

        var allNotes = await noteRepository.GetNotesAsync();
        Notes = new ObservableCollection<Note>(allNotes);

        foreach (var n in Notes)
            n.PropertyChanged += Note_PropertyChanged;

        HasSelectedItems = Notes.Any(n => n.IsSelected);
        SelectedNote = Notes.FirstOrDefault(n => n.IsSelected);
    }

    private void Note_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Note.IsSelected))
        {
            HasSelectedItems = Notes.Any(n => n.IsSelected);
            SelectedNote = Notes.FirstOrDefault(n => n.IsSelected);
        }
    }

    [RelayCommand]
    public async Task AddNewNoteAsync()
    {
        await Shell.Current.GoToAsync("notedetail");
    }

    [RelayCommand]
    public async Task DeleteSelectedAsync()
    {
        var toDelete = Notes.Where(n => n.IsSelected).ToList();
        if (!toDelete.Any())
        {
            await Shell.Current.DisplayAlert("Törlés", "Nincs kijelölt jegyzet.", "OK");
            return;
        }

        var confirm = await Shell.Current.DisplayAlert(
            "Törlés megerősítése",
            $"Biztosan törlöd a {toDelete.Count} kijelölt jegyzetet?",
            "Törlés",
            "Mégse");

        if (!confirm) return;

        foreach (var note in toDelete)
        {
            await noteRepository.DeleteNoteAsync(note);
        }

        IsSelectionMode = false;
        foreach (var n in Notes) n.IsSelected = false;
        HasSelectedItems = false;
        SelectedNote = null;

        await LoadNotesAsync();
    }

    [RelayCommand]
    public async Task OpenNoteAsync(Note note)
    {
        if (note == null) return;

        await Shell.Current.GoToAsync("notedetail");
        WeakReferenceMessenger.Default.Send(new NoteSelectedMessage(note));
    }

    [RelayCommand]
    public void SelectNote(Note note)
    {
        if (note == null) return;

        if (SelectedNote != null && SelectedNote != note)
            SelectedNote.IsSelected = false;

        var newState = !note.IsSelected;
        note.IsSelected = newState;

        SelectedNote = newState ? note : null;
    }
}
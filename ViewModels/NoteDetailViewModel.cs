using AdvancedNoteApp.Models;
using AdvancedNoteApp.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AdvancedNoteApp.Messages;
using AdvancedNoteApp.Services;

namespace AdvancedNoteApp.ViewModels;

[QueryProperty(nameof(Note), "Note")]
public partial class NoteDetailViewModel : ObservableObject
{
    private readonly NoteRepository noteRepository;
    private readonly MediaService mediaService;

    [ObservableProperty]
    private Note note = new Note();

    public NoteDetailViewModel(NoteRepository noteRepository, MediaService mediaService)
    {
        this.noteRepository = noteRepository;
        this.mediaService = mediaService;

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

    [RelayCommand]
    public async Task CaptureImageAsync()
    {
        var path = await mediaService.CapturePhotoAsync();
        if (!string.IsNullOrEmpty(path))
        {
            Note.ImageUrl = path;
            await SaveNoteAsync();
        }
    }

    [RelayCommand]
    public async Task RemoveImageAsync()
    {
        if (string.IsNullOrEmpty(Note?.ImageUrl)) return;
        Note.ImageUrl = null;
        await SaveNoteAsync();
    }
}

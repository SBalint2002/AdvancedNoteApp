using AdvancedNoteApp.Models;
using AdvancedNoteApp.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AdvancedNoteApp.Messages;
using AdvancedNoteApp.Services;
using System.ComponentModel;

namespace AdvancedNoteApp.ViewModels;

[QueryProperty(nameof(Note), "Note")]
public partial class NoteDetailViewModel : ObservableObject
{
    private readonly NoteRepository noteRepository;
    private readonly MediaService mediaService;
    private readonly SemaphoreSlim saveLock = new(1, 1);
    private Note? previousNote;

    [ObservableProperty]
    private Note note = new Note();

    [ObservableProperty]
    private bool hasImage = false;

    public NoteDetailViewModel(NoteRepository noteRepository, MediaService mediaService)
    {
        this.noteRepository = noteRepository;
        this.mediaService = mediaService;

        WeakReferenceMessenger.Default.Register<NoteSelectedMessage>(this, (r, m) =>
        {
            Note = m.Value;
        });

        UpdateHasImage();
    }

    partial void OnNoteChanged(Note value)
    {
        if (previousNote is not null)
            previousNote.PropertyChanged -= Note_PropertyChanged;

        previousNote = value;

        if (previousNote is not null)
            previousNote.PropertyChanged += Note_PropertyChanged;

        UpdateHasImage();
    }

    private void Note_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e?.PropertyName == nameof(Note.ImageUrl))
        {
            UpdateHasImage();
        }
    }

    private void UpdateHasImage()
    {
        HasImage = !string.IsNullOrEmpty(Note?.ImageUrl);
    }

    [RelayCommand]
    public async Task SaveNoteAsync()
    {
        if (Note == null) return;

        await saveLock.WaitAsync();
        try
        {
            await noteRepository.SaveNoteAsync(Note);
            WeakReferenceMessenger.Default.Send(new NoteSavedMessage(Note));
        }
        finally
        {
            saveLock.Release();
        }
    }

    [RelayCommand]
    public async Task CaptureImageAsync()
    {
        try
        {
            var path = await mediaService.CapturePhotoAsync();
            if (string.IsNullOrEmpty(path))
                return;

            Note.ImageUrl = path;
            UpdateHasImage();

            await SaveNoteAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Hiba", "Kép készítése nem sikerült: " + ex.Message, "OK");
        }
    }

    [RelayCommand]
    public async Task RemoveImageAsync()
    {
        if (string.IsNullOrEmpty(Note?.ImageUrl)) return;
        Note.ImageUrl = null;
        UpdateHasImage();
        await SaveNoteAsync();
    }
}

using AdvancedNoteApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace AdvancedNoteApp.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISyncService syncService;
    public SettingsViewModel(ISyncService syncService)
    {
        this.syncService = syncService ?? throw new ArgumentException(nameof(syncService));
    }

    [ObservableProperty]
    private bool isSyncing = false;

    [RelayCommand]
    public async Task SyncAsync()
    {
        if (isSyncing) return;
        try
        {
            isSyncing = true;
            await syncService.SyncAllNotesAsync();
            WeakReferenceMessenger.Default.Send("Szinkronizáció befejezve");
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send($"Hiba a szinkronizáció során: {ex.Message}");
        }
        finally
        {
            isSyncing = false;
        }
    }
}
using Microsoft.Extensions.Logging;

namespace AdvancedNoteApp.Services;

internal class NetworkMonitor : IDisposable
{
    private readonly ISyncService syncService;
    private readonly ILogger<NetworkMonitor> logger;
    private bool hasInternet;

    public NetworkMonitor(ISyncService syncService, ILogger<NetworkMonitor> logger)
    {
        this.syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        hasInternet = Connectivity.NetworkAccess == NetworkAccess.Internet;
        Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;

        if (hasInternet)
        {
            _ = TriggerSyncAsync();
        }
    }

    private void Connectivity_ConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        var nowHasInternet = e.NetworkAccess == NetworkAccess.Internet;

        if (!hasInternet && nowHasInternet)
        {
            _ = TriggerSyncAsync();
        }

        hasInternet = nowHasInternet;
    }

    private async Task TriggerSyncAsync()
    {
        try
        {
            logger.LogInformation("Internet connection restored. Triggering sync...");
            await syncService.SyncAllNotesAsync();
            logger.LogInformation("Sync completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during sync.");
        }
    }

    public void Dispose()
    {
        Connectivity.ConnectivityChanged -= Connectivity_ConnectivityChanged;
    }
}

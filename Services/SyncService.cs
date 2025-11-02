using AdvancedNoteApp.Models;
using Microsoft.Extensions.Logging;
using static Supabase.Postgrest.Constants;

namespace AdvancedNoteApp.Services;

public class SyncService : ISyncService
{
    private readonly ILocalDatabase localDb;
    private readonly Supabase.Client supabase;
    private readonly ILogger<SyncService> logger;

    private bool isSyncing;
    private readonly object syncLock = new();

    public SyncService(ILocalDatabase localDb, Supabase.Client supabase, ILogger<SyncService> logger)
    {
        this.localDb = localDb ?? throw new ArgumentNullException(nameof(localDb));
        this.supabase = supabase ?? throw new ArgumentNullException(nameof(supabase));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SyncAllNotesAsync()
    {
        lock (syncLock)
        {
            if (isSyncing) return;
            isSyncing = true;
        }

        try
        {
            await ProcessDeletedNotesAsync();
            await UploadUnsyncedNotesAsync();
            await PullRemoteNotesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unhandled error during sync");
        }
        finally
        {
            lock (syncLock) { isSyncing = false; }
        }
    }

    private async Task ProcessDeletedNotesAsync()
    {
        var localNotes = await localDb.GetAllNotesAsync();
        var deleted = localNotes.Where(n => n.Deleted).ToList();
        if (!deleted.Any()) return;

        foreach (var d in deleted)
        {
            try
            {
                await supabase
                    .From<RemoteNote>()
                    .Filter("local_id", Operator.Equals, d.Id)
                    .Delete();

                await localDb.RemoveNoteAsync(d);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error deleting remote note {Id}", d.Id);
            }
        }
    }

    private async Task UploadUnsyncedNotesAsync()
    {
        var localNotes = await localDb.GetAllNotesAsync();
        var toUpload = localNotes.Where(n => !n.Synced && !n.Deleted).ToList();
        if (!toUpload.Any()) return;

        foreach (var note in toUpload)
        {
            var ok = await UploadSingleNoteAsync(note);
            if (!ok)
                logger.LogWarning("Upload failed for note {Id}", note.Id);
        }
    }

    private async Task<bool> UploadSingleNoteAsync(Note note)
    {
        try
        {
            var remote = NoteMapper.ToRemote(note);

            var existingResp = await supabase
                .From<RemoteNote>()
                .Filter("local_id", Operator.Equals, note.Id)
                .Get();

            RemoteNote? returned = null;

            if (existingResp.Models != null && existingResp.Models.Count > 0)
            {
                var existing = existingResp.Models.Cast<RemoteNote>().First();
                remote.Id = existing.Id;

                var updateResp = await supabase
                    .From<RemoteNote>()
                    .Filter("local_id", Operator.Equals, note.Id)
                    .Update(remote);

                if (updateResp.Models != null && updateResp.Models.Count > 0)
                    returned = updateResp.Models.Cast<RemoteNote>().First();
            }
            else
            {
                var insertResp = await supabase
                    .From<RemoteNote>()
                    .Insert(remote);

                if (insertResp.Models != null && insertResp.Models.Count > 0)
                    returned = insertResp.Models.Cast<RemoteNote>().First();
            }

            if (returned != null)
            {
                var mapped = NoteMapper.FromRemote(returned);
                mapped.Synced = true;
                await localDb.UpsertNoteAsync(mapped);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Network/upload error for note {Id}", note.Id);
            return false;
        }
    }

    private async Task PullRemoteNotesAsync()
    {
        try
        {
            var response = await supabase
                .From<RemoteNote>()
                .Get();

            var remoteNotes = response.Models ?? new List<RemoteNote>();

            if (remoteNotes.Count == 0)
            {
                logger.LogWarning("Remote pull failed: no data returned from Supabase.");
                return;
            }

            var localAll = await localDb.GetAllNotesAsync();
            var localById = localAll.ToDictionary(n => n.Id);

            foreach (var r in remoteNotes)
            {
                var mapped = NoteMapper.FromRemote(r);

                if (localById.TryGetValue(r.LocalId, out var local))
                {
                    if (r.UpdatedAt > local.UpdatedAt)
                    {
                        mapped.Synced = true;
                        mapped.Deleted = false;
                        await localDb.UpsertNoteAsync(mapped);
                    }
                }
                else
                {
                    mapped.Synced = true;
                    mapped.Deleted = false;
                    await localDb.UpsertNoteAsync(mapped);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error pulling remote notes via Supabase SDK");
        }
    }
}
using AdvancedNoteApp.Models;
using Microsoft.Extensions.Logging;
using static Supabase.Postgrest.Constants;
using CommunityToolkit.Mvvm.Messaging;
using AdvancedNoteApp.Messages;

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
            await SyncDeletedAsync();
            await SyncRemoteToLocalAsync();
            await SyncLocalToRemoteAsync();
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

    private async Task SyncDeletedAsync()
    {
        var localNotes = await localDb.GetAllNotesAsync();
        var deleted = localNotes.Where(n => n.Deleted).ToList();
        if (!deleted.Any()) return;

        foreach (var d in deleted)
        {
            try
            {
                await DeleteRemoteByLocalIdAsync(d.Id);
                await localDb.RemoveNoteAsync(d);

                WeakReferenceMessenger.Default.Send(new NoteSavedMessage(d));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error deleting remote note {Id}", d.Id);
            }
        }
    }

    private async Task SyncRemoteToLocalAsync()
    {
        var remoteList = await FetchAllRemoteNotesAsync();
        if (!remoteList.Any()) return;

        var localAll = await localDb.GetAllNotesAsync();
        var localById = localAll.ToDictionary(n => n.Id);
        var localByRemoteId = localAll.Where(n => n.RemoteId is not null)
                                      .ToDictionary(n => n.RemoteId!, n => n);

        foreach (var r in remoteList)
        {
            try
            {
                await MergeRemoteAsync(r, localById, localByRemoteId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error merging remote note {RemoteId}", r.Id);
            }
        }
    }

    private async Task SyncLocalToRemoteAsync()
    {
        var remoteList = await FetchAllRemoteNotesAsync();
        var remoteByLocal = remoteList.Where(r => r.LocalId.HasValue)
                                      .ToDictionary(r => r.LocalId!.Value, r => r);

        var localNotes = await localDb.GetAllNotesAsync();
        var toUpload = localNotes.Where(n => !n.Synced && !n.Deleted).ToList();
        if (!toUpload.Any()) return;

        foreach (var note in toUpload)
        {
            try
            {
                await UploadSingleLocalAsync(note, remoteByLocal);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Upload error for local note {Id}", note.Id);
            }
        }
    }

    private async Task<List<RemoteNote>> FetchAllRemoteNotesAsync()
    {
        try
        {
            var resp = await supabase.From<RemoteNote>().Get();
            return resp.Models?.Cast<RemoteNote>().ToList() ?? new List<RemoteNote>();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch remote notes");
            return new List<RemoteNote>();
        }
    }

    private async Task DeleteRemoteByLocalIdAsync(int localId)
    {
        await supabase
            .From<RemoteNote>()
            .Filter("local_id", Operator.Equals, localId)
            .Delete();
    }

    private async Task MergeRemoteAsync(RemoteNote r, Dictionary<int, Note> localById, Dictionary<string, Note> localByRemoteId)
    {
        if (r.LocalId.HasValue)
        {
            if (localById.TryGetValue(r.LocalId.Value, out var local))
            {
                await SyncLocalAndRemoteAsync(local, r);
                return;
            }

            var createdLocal = CreateLocalFromRemote(r);
            await localDb.SaveNoteAsync(createdLocal, markAsSynced: true);

            WeakReferenceMessenger.Default.Send(new NoteSavedMessage(createdLocal));

            if (!string.IsNullOrEmpty(r.Id))
            {
                await UpdateRemoteLocalIdAsync(r.Id!, createdLocal.Id);
            }

            return;
        }

        if (!string.IsNullOrEmpty(r.Id) && localByRemoteId.TryGetValue(r.Id!, out var existingLocal))
        {
            await UpdateRemoteLocalIdAsync(r.Id!, existingLocal.Id);

            if (r.UpdatedAt > existingLocal.UpdatedAt)
            {
                var mapped = NoteMapper.FromRemote(r);
                mapped.Synced = true;
                mapped.Deleted = false;
                mapped.Id = existingLocal.Id;
                mapped.RemoteId = existingLocal.RemoteId;
                await localDb.SaveNoteAsync(mapped, markAsSynced: true);

                WeakReferenceMessenger.Default.Send(new NoteSavedMessage(mapped));
            }

            return;
        }

        var mappedInsert = CreateLocalFromRemote(r);
        await localDb.SaveNoteAsync(mappedInsert, markAsSynced: true);

        WeakReferenceMessenger.Default.Send(new NoteSavedMessage(mappedInsert));

        if (!string.IsNullOrEmpty(r.Id))
        {
            await UpdateRemoteLocalIdAsync(r.Id!, mappedInsert.Id);
        }
    }

    private Note CreateLocalFromRemote(RemoteNote r)
    {
        var mapped = NoteMapper.FromRemote(r);
        mapped.Synced = true;
        mapped.Deleted = false;
        mapped.RemoteId = r.Id;
        return mapped;
    }

    private async Task SyncLocalAndRemoteAsync(Note local, RemoteNote r)
    {
        if (!string.IsNullOrEmpty(r.Id) && local.RemoteId != r.Id)
        {
            local.RemoteId = r.Id;
        }

        if (r.UpdatedAt > local.UpdatedAt)
        {
            var mapped = NoteMapper.FromRemote(r);
            mapped.Synced = true;
            mapped.Deleted = false;
            mapped.Id = local.Id;
            mapped.RemoteId = r.Id;
            await localDb.SaveNoteAsync(mapped, markAsSynced: true);

            WeakReferenceMessenger.Default.Send(new NoteSavedMessage(mapped));

            return;
        }

        if (local.UpdatedAt > r.UpdatedAt)
        {
            var remoteUpdate = NoteMapper.ToRemote(local);
            remoteUpdate.Id = r.Id;
            remoteUpdate.LocalId = local.Id;

            if (!string.IsNullOrEmpty(r.Id))
            {
                await UpdateRemoteByIdAsync(r.Id!, remoteUpdate);
            }

            local.Synced = true;
            await localDb.SaveNoteAsync(local, markAsSynced: true);

            WeakReferenceMessenger.Default.Send(new NoteSavedMessage(local));
        }
    }

    private async Task UpdateRemoteLocalIdAsync(string remoteId, int localId)
    {
        try
        {
            await supabase
                .From<RemoteNote>()
                .Filter("id", Operator.Equals, remoteId)
                .Update(new RemoteNote { LocalId = localId });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update remote LocalId for remote {RemoteId}", remoteId);
        }
    }

    private async Task UpdateRemoteByIdAsync(string remoteId, RemoteNote remote)
    {
        try
        {
            await supabase
                .From<RemoteNote>()
                .Filter("id", Operator.Equals, remoteId)
                .Update(remote);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update remote {RemoteId}", remoteId);
        }
    }

    private async Task UploadSingleLocalAsync(Note note, Dictionary<int, RemoteNote> remoteByLocal)
    {
        var remote = NoteMapper.ToRemote(note);
        remote.LocalId = note.Id;

        if (!string.IsNullOrEmpty(note.RemoteId))
        {
            remote.Id = note.RemoteId;
            await UpdateRemoteByIdAsync(remote.Id!, remote);
        }
        else if (remoteByLocal.TryGetValue(note.Id, out var existing))
        {
            remote.Id = existing.Id;
            if (!string.IsNullOrEmpty(existing.Id))
            {
                await UpdateRemoteByIdAsync(existing.Id!, remote);
                if (string.IsNullOrEmpty(note.RemoteId))
                {
                    note.RemoteId = existing.Id;
                }
            }
        }
        else
        {
            try
            {
                var insertResp = await supabase
                    .From<RemoteNote>()
                    .Insert(remote);

                var created = insertResp.Models?.Cast<RemoteNote>().FirstOrDefault();
                if (created is not null)
                {
                    note.RemoteId = created.Id;

                    if (!created.LocalId.HasValue && !string.IsNullOrEmpty(created.Id))
                    {
                        await UpdateRemoteLocalIdAsync(created.Id!, note.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to insert remote for local note {Id}", note.Id);
            }
        }

        note.Synced = true;
        await localDb.SaveNoteAsync(note, markAsSynced: true);

        WeakReferenceMessenger.Default.Send(new NoteSavedMessage(note));
    }
}
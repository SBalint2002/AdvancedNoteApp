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
            await PullAndMergeRemoteAsync();
            await UploadLocalChangesAsync();
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

    private async Task PullAndMergeRemoteAsync()
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
                await HandleSingleRemoteAsync(r, localById, localByRemoteId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error merging remote note {RemoteId}", r.Id);
            }
        }
    }

    private async Task HandleSingleRemoteAsync(RemoteNote r, Dictionary<int, Note> localById, Dictionary<string, Note> localByRemoteId)
    {
        if (r.LocalId.HasValue)
        {
            if (localById.TryGetValue(r.LocalId.Value, out var local))
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
                    await localDb.SaveNoteAsync(mapped);
                }
                else if (local.UpdatedAt > r.UpdatedAt)
                {
                    var remoteUpdate = NoteMapper.ToRemote(local);
                    remoteUpdate.Id = r.Id;
                    remoteUpdate.LocalId = local.Id;
                    if (!string.IsNullOrEmpty(r.Id))
                    {
                        await supabase
                            .From<RemoteNote>()
                            .Filter("id", Operator.Equals, r.Id)
                            .Update(remoteUpdate);
                    }

                    local.Synced = true;
                    await localDb.SaveNoteAsync(local);
                }

                return;
            }

            var createdLocal = NoteMapper.FromRemote(r);
            createdLocal.Synced = true;
            createdLocal.Deleted = false;
            createdLocal.RemoteId = r.Id;
            await localDb.SaveNoteAsync(createdLocal);

            if (!string.IsNullOrEmpty(r.Id))
            {
                await supabase
                    .From<RemoteNote>()
                    .Filter("id", Operator.Equals, r.Id)
                    .Update(new RemoteNote { LocalId = createdLocal.Id });
            }

            return;
        }

        if (!string.IsNullOrEmpty(r.Id) && localByRemoteId.TryGetValue(r.Id!, out var existingLocal))
        {
            await supabase
                .From<RemoteNote>()
                .Filter("id", Operator.Equals, r.Id)
                .Update(new RemoteNote { LocalId = existingLocal.Id });

            if (r.UpdatedAt > existingLocal.UpdatedAt)
            {
                var mapped = NoteMapper.FromRemote(r);
                mapped.Synced = true;
                mapped.Deleted = false;
                mapped.Id = existingLocal.Id;
                mapped.RemoteId = existingLocal.RemoteId;
                await localDb.SaveNoteAsync(mapped);
            }

            return;
        }

        var mappedInsert = NoteMapper.FromRemote(r);
        mappedInsert.Synced = true;
        mappedInsert.Deleted = false;
        mappedInsert.RemoteId = r.Id;
        await localDb.SaveNoteAsync(mappedInsert);

        if (!string.IsNullOrEmpty(r.Id))
        {
            await supabase
                .From<RemoteNote>()
                .Filter("id", Operator.Equals, r.Id)
                .Update(new RemoteNote { LocalId = mappedInsert.Id });
        }
    }

    private async Task UploadLocalChangesAsync()
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
                var remote = NoteMapper.ToRemote(note);
                remote.LocalId = note.Id;

                if (!string.IsNullOrEmpty(note.RemoteId))
                {
                    remote.Id = note.RemoteId;
                    await supabase
                        .From<RemoteNote>()
                        .Filter("id", Operator.Equals, remote.Id)
                        .Update(remote);
                }
                else if (remoteByLocal.TryGetValue(note.Id, out var existing))
                {
                    remote.Id = existing.Id;
                    if (!string.IsNullOrEmpty(existing.Id))
                    {
                        await supabase
                            .From<RemoteNote>()
                            .Filter("id", Operator.Equals, existing.Id)
                            .Update(remote);

                        if (string.IsNullOrEmpty(note.RemoteId))
                        {
                            note.RemoteId = existing.Id;
                        }
                    }
                }
                else
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
                            await supabase
                                .From<RemoteNote>()
                                .Filter("id", Operator.Equals, created.Id)
                                .Update(new RemoteNote { LocalId = note.Id });
                        }
                    }
                }

                note.Synced = true;
                await localDb.SaveNoteAsync(note);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Upload error for local note {Id}", note.Id);
            }
        }
    }
}
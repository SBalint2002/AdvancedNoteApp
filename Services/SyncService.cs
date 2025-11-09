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
            var allLocal = await localDb.GetAllNotesAsync();
            var allRemote = await FetchAllRemoteNotesAsync();

            await PushDeletedAsync(allLocal, allRemote);
            await PullRemoteAsync(allLocal, allRemote);
            await PushLocalAsync(allLocal, allRemote);
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

    private async Task PushDeletedAsync(List<Note> local, List<RemoteNote> remote)
    {
        var deleted = local.Where(n => n.Deleted).ToList();
        foreach (var d in deleted)
        {
            if (string.IsNullOrEmpty(d.RemoteId))
            {
                await localDb.RemoveNoteAsync(d);
                continue;
            }

            try
            {
                await supabase.From<RemoteNote>()
                    .Filter("id", Operator.Equals, d.RemoteId)
                    .Delete();

                await localDb.RemoveNoteAsync(d);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Delete failed for {Id}", d.Id);
            }
        }
    }

    private async Task PullRemoteAsync(List<Note> local, List<RemoteNote> remote)
    {
        var localByRemoteId = local.Where(n => !string.IsNullOrEmpty(n.RemoteId))
                                   .ToDictionary(n => n.RemoteId!, n => n);

        foreach (var r in remote)
        {
            if (localByRemoteId.TryGetValue(r.Id, out var localNote))
            {
                if (r.UpdatedAt > localNote.UpdatedAt)
                {
                    var updated = NoteMapper.FromRemote(r);
                    updated.Id = localNote.Id;
                    updated.RemoteId = r.Id;
                    await localDb.UpdateNoteAsync(updated, synced: true);
                }
            }
            else
            {
                var newNote = NoteMapper.FromRemote(r);
                newNote.RemoteId = r.Id;
                await localDb.InsertNoteAsync(newNote);
                await localDb.UpdateNoteAsync(newNote, synced: true);
            }
        }
    }

    private async Task PushLocalAsync(List<Note> local, List<RemoteNote> remote)
    {
        var remoteById = remote.ToDictionary(r => r.Id);

        foreach (var note in local.Where(n => !n.Deleted && !n.Synced))
        {
            try
            {
                if (!string.IsNullOrEmpty(note.RemoteId) && remoteById.ContainsKey(note.RemoteId))
                {
                    var remoteUpdate = NoteMapper.ToRemote(note);
                    await supabase.From<RemoteNote>()
                        .Filter("id", Operator.Equals, note.RemoteId)
                        .Update(remoteUpdate);
                }
                else
                {
                    var insertResp = await supabase.From<RemoteNote>().Insert(NoteMapper.ToRemote(note));
                    var created = insertResp.Models?.Cast<RemoteNote>().FirstOrDefault();
                    if (created is not null)
                        note.RemoteId = created.Id;
                }

                await localDb.UpdateNoteAsync(note, synced: true);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Upload failed for {Id}", note.Id);
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
}
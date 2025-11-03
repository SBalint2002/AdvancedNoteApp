namespace AdvancedNoteApp.Models;

public static class NoteMapper
{
    public static RemoteNote ToRemote(Note note)
    {
        if (note is null) throw new ArgumentNullException(nameof(note));

        return new RemoteNote
        {
            Id = note.RemoteId,
            LocalId = note.Id,
            Title = note.Title,
            Content = note.Content,
            ImageUrl = note.ImageUrl,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt
        };
    }

    public static Note FromRemote(RemoteNote rn)
    {
        if (rn is null) throw new ArgumentNullException(nameof(rn));

        return new Note
        {
            Id = rn.LocalId ?? 0,
            RemoteId = rn.Id,
            Title = rn.Title ?? string.Empty,
            Content = rn.Content ?? string.Empty,
            ImageUrl = rn.ImageUrl,
            CreatedAt = rn.CreatedAt == default ? DateTime.UtcNow : rn.CreatedAt,
            UpdatedAt = rn.UpdatedAt == default ? DateTime.UtcNow : rn.UpdatedAt,
            Synced = true,
            Deleted = false
        };
    }
}

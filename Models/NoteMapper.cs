namespace AdvancedNoteApp.Models;

public static class NoteMapper
{
    public static RemoteNote ToRemote(Note n) => new RemoteNote
    {
        Id = null,
        LocalId = n.Id,
        Title = n.Title,
        Content = n.Content,
        ImageUrl = n.ImageUrl,
        CreatedAt = n.CreatedAt,
        UpdatedAt = n.UpdatedAt
    };

    public static Note FromRemote(RemoteNote rn) => new Note
    {
        Id = rn.LocalId,
        Title = rn.Title ?? string.Empty,
        Content = rn.Content ?? string.Empty,
        ImageUrl = rn.ImageUrl,
        CreatedAt = rn.CreatedAt == default ? DateTime.UtcNow : rn.CreatedAt,
        UpdatedAt = rn.UpdatedAt == default ? DateTime.UtcNow : rn.UpdatedAt,
        Synced = true,
        Deleted = false
    };
}

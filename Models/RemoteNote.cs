using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AdvancedNoteApp.Models;

[Table("notes")]
public class RemoteNote : BaseModel
{
    [PrimaryKey("id", false)]
    public string? Id { get; set; }

    [Column("local_id")]
    public int LocalId { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

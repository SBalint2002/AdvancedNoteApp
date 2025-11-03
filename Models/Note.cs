using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace AdvancedNoteApp.Models
{
    public partial class Note : ObservableObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Column("remote_id")]
        public string? RemoteId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool Synced { get; set; } = false;
        public bool Deleted { get; set; } = false;

        [property: SQLite.Ignore]
        [ObservableProperty]
        private bool isSelected = false;
    }
}
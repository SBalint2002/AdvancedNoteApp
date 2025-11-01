using AdvancedNoteApp.Models;
using AdvancedNoteApp.Repositories;

namespace AdvancedNoteApp.Services;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<NoteRepository>();

        var existing = await repo.GetNotesAsync();
        if (existing is { Count: > 0 })
            return;

        var now = DateTime.UtcNow;
        var samples = new List<Note>
            {
                new Note { Title = "Első jegyzet", Content = "Ez egy példa jegyzet. Szerkeszd, töröld vagy hozz létre újat!", CreatedAt = now, UpdatedAt = now },
                new Note { Title = "Bevásárló lista", Content = "• Tej\n• Kenyér\n• Tojás", CreatedAt = now, UpdatedAt = now },
                new Note { Title = "Ötletek", Content = "Ötletek xd", CreatedAt = now, UpdatedAt = now }
            };

        foreach (var n in samples)
        {
            await repo.SaveNoteAsync(n);
        }
    }
}

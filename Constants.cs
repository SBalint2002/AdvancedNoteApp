namespace AdvancedNoteApp;

public static class Constants
{
    public const string DatabaseFileName = "AdvancedNoteApp.db";
    public const SQLite.SQLiteOpenFlags Flags =
        SQLite.SQLiteOpenFlags.ReadWrite |
        SQLite.SQLiteOpenFlags.Create |
        SQLite.SQLiteOpenFlags.SharedCache;
    public static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);

    public const string SupabaseUrl = "https://kajsdgunuqyjsapaynwt.supabase.co";
    public const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImthanNkZ3VudXF5anNhcGF5bnd0Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjAyODIxMjUsImV4cCI6MjA3NTg1ODEyNX0.u_yd_IUXWoGYoC7xMgAO_-DEfwEt3EmbU5XQf5T6rIY";
}

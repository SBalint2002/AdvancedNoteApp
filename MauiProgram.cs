using AdvancedNoteApp.Repositories;
using AdvancedNoteApp.Services;
using AdvancedNoteApp.ViewModels;
using AdvancedNoteApp.Views;
using Microsoft.Extensions.Logging;

namespace AdvancedNoteApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            var supabaseClient = new Supabase.Client(Constants.SupabaseUrl, Constants.SupabaseAnonKey);
            supabaseClient.InitializeAsync().GetAwaiter().GetResult();
            builder.Services.AddSingleton(supabaseClient);

            builder.Services.AddSingleton<NoteRepository>();
            builder.Services.AddSingleton<ILocalDatabase, LocalDatabase>();
            builder.Services.AddSingleton<ISyncService, SyncService>();

            builder.Services.AddTransient<NotesListViewModel>();
            builder.Services.AddTransient<NoteDetailViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();

            builder.Services.AddTransient<NotesListPage>();
            builder.Services.AddTransient<NoteDetailPage>();
            builder.Services.AddTransient<SettingsPage>();
            
            builder.Services.AddSingleton<NetworkMonitor>();
            builder.Services.AddSingleton<MediaService>();
#if DEBUG
            builder.Logging.AddDebug();
#endif
            var mauiApp = builder.Build();

            mauiApp.Services.GetRequiredService<NetworkMonitor>();

            return mauiApp;
        }
    }
}

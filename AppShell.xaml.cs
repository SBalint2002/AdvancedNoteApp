using AdvancedNoteApp.Views;

namespace AdvancedNoteApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("notedetail", typeof(NoteDetailPage));
        }
    }
}

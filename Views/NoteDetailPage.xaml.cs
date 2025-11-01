using AdvancedNoteApp.ViewModels;

namespace AdvancedNoteApp.Views;

public partial class NoteDetailPage : ContentPage
{
    private readonly NoteDetailViewModel viewModel;
    private bool isSaving;

    public NoteDetailPage(NoteDetailViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        BindingContext = viewModel;
    }

    private async void OnBackTapped(object sender, EventArgs e)
        => await SaveAndGoBackAsync();

    private async Task SaveAndGoBackAsync()
    {
        if (isSaving)
            return;

        try
        {
            isSaving = true;
            await viewModel.SaveNoteAsync();
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            isSaving = false;
        }
    }
}

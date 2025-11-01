using AdvancedNoteApp.ViewModels;
using CommunityToolkit.Mvvm.Messaging;

namespace AdvancedNoteApp.Views;

public partial class NotesListPage : ContentPage
{
    private readonly NotesListViewModel viewModel;

    public NotesListPage(NotesListViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        BindingContext = viewModel;
        WeakReferenceMessenger.Default.Register<string>(this, async (r, m) =>
        {
            await DisplayAlert("Warning", m, "OK");
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.LoadNotesAsync();
    }
}

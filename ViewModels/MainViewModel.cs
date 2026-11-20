using CommunityToolkit.Mvvm.ComponentModel;
using JobApplicationManager.Context;
using JobApplicationManager.Services;

namespace JobApplicationManager.ViewModels
{
    /// <summary>
    /// Pääikkunan ViewModel: kokoaa välilehtien ViewModelit. Kaikki jakavat saman AppDbContextin,
    /// joten muutokset näkyvät heti kaikissa näkymissä.
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        public MainViewModel(AppDbContext db, IDialogService dialogs)
        {
            Applications = new ApplicationsViewModel(db, dialogs);
        }

        public ApplicationsViewModel Applications { get; }
    }
}

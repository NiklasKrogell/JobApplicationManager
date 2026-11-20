using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobApplicationManager.Context;
using JobApplicationManager.Model;
using JobApplicationManager.Services;

namespace JobApplicationManager.ViewModels
{
    /// <summary>
    /// Pääikkunan ViewModel: kokoaa välilehtien ViewModelit ja välittää pikanäppäimet avoimelle välilehdelle.
    /// Kaikki jakavat saman AppDbContextin, joten muutokset näkyvät heti kaikissa näkymissä.
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        public const int ApplicationsTab = 0;
        public const int CompaniesTab = 1;
        public const int StatisticsTab = 2;

        private readonly AppDbContext _db;
        private readonly IDialogService _dialogs;

        public MainViewModel(AppDbContext db, IDialogService dialogs)
        {
            _db = db;
            _dialogs = dialogs;
            Applications = new ApplicationsViewModel(db, dialogs);
            Companies = new CompaniesViewModel(db, dialogs);
            Companies.OpenApplicationRequested += OnOpenApplicationRequested;
            Statistics = new StatisticsViewModel(db);
        }

        public ApplicationsViewModel Applications { get; }

        public CompaniesViewModel Companies { get; }

        public StatisticsViewModel Statistics { get; }

        [ObservableProperty]
        private int selectedTabIndex = ApplicationsTab;

        partial void OnSelectedTabIndexChanged(int value)
        {
            switch (value)
            {
                case ApplicationsTab: Applications.Activate(); break;
                case CompaniesTab: Companies.Activate(); break;
                case StatisticsTab: Statistics.Refresh(); break;
            }
        }

        /// <summary>Ctrl+S: tallentaa (yhteinen konteksti) ja päivittää avoimen välilehden listat.</summary>
        [RelayCommand]
        private void Save()
        {
            if (SelectedTabIndex == CompaniesTab)
                Companies.SaveCommand.Execute(null);
            else
                Applications.SaveCommand.Execute(null);
        }

        /// <summary>Ctrl+N: uusi hakemus tai uusi yritys sen mukaan, mikä välilehti on auki.</summary>
        [RelayCommand]
        private void New()
        {
            switch (SelectedTabIndex)
            {
                case ApplicationsTab: Applications.NewApplicationCommand.Execute(null); break;
                case CompaniesTab: Companies.NewCompanyCommand.Execute(null); break;
            }
        }

        /// <summary>
        /// Kysytään ikkunaa suljettaessa: tallentamattomat muutokset tallennetaan, hylätään tai sulkeminen perutaan.
        /// Palauttaa false, jos ikkuna pitää jättää auki.
        /// </summary>
        public bool ConfirmClose()
        {
            if (!_db.ChangeTracker.HasChanges())
                return true;

            var answer = _dialogs.AskYesNoCancel(
                "Sinulla on tallentamattomia muutoksia.\n\nTallennetaanko ne ennen sulkemista?",
                "Tallentamattomat muutokset");

            return answer switch
            {
                true => _db.TrySave(_dialogs),   // epäonnistunut tallennus (esim. puuttuva tieto) jättää ikkunan auki
                false => true,
                null => false
            };
        }

        private void OnOpenApplicationRequested(JobApplication application)
        {
            Applications.ShowApplication(application);
            SelectedTabIndex = ApplicationsTab;
        }
    }
}

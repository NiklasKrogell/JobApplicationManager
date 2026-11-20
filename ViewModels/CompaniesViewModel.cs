using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobApplicationManager.Context;
using JobApplicationManager.Model;
using JobApplicationManager.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.ComponentModel;
using System.Windows.Data;

namespace JobApplicationManager.ViewModels
{
    /// <summary>
    /// Yritykset-välilehti: yrityslista vasemmalla, valitun yrityksen tiedot ja hakemukset keskellä,
    /// toimialojen ylläpito oikealla.
    /// Yrityksen muokkaukset tallentuvat Tallenna-komennolla; toimialojen lisäys, uudelleennimeäminen
    /// ja poisto tallentuvat heti, koska niillä on omat nappinsa.
    /// </summary>
    public partial class CompaniesViewModel : ObservableObject
    {
        private readonly AppDbContext _db;
        private readonly IDialogService _dialogs;

        public CompaniesViewModel(AppDbContext db, IDialogService dialogs)
        {
            _db = db;
            _dialogs = dialogs;

            // Include merkitsee navigaatiokokoelmat ladatuiksi, jolloin lukumäärät ja yrityksen hakemuslista
            // tulevat suoraan muistista eikä lazy loading tee kyselyä jokaiselle riville erikseen.
            _db.Industries.Include(i => i.Companies).Load();
            _db.Companies.Include(c => c.JobApplications).Load();

            CompaniesView = new ListCollectionView(_db.Companies.Local.ToObservableCollection()) { Filter = FilterCompany };
            CompaniesView.SortDescriptions.Add(new SortDescription(nameof(Company.Name), ListSortDirection.Ascending));

            IndustriesView = new ListCollectionView(_db.Industries.Local.ToObservableCollection());
            IndustriesView.SortDescriptions.Add(new SortDescription(nameof(Industry.Name), ListSortDirection.Ascending));
        }

        /// <summary>Pyytää pääikkunaa näyttämään hakemuksen Hakemukset-välilehdellä.</summary>
        public event Action<JobApplication>? OpenApplicationRequested;

        /// <summary>Yrityslista: aakkosjärjestys + haku nimestä ja toimialasta.</summary>
        public ListCollectionView CompaniesView { get; }

        /// <summary>Toimialat aakkosjärjestyksessä (toimialalista ja yrityksen toimialavalinta).</summary>
        public ListCollectionView IndustriesView { get; }

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DeleteCompanyCommand))]
        [NotifyCanExecuteChangedFor(nameof(ClearIndustryCommand))]
        private Company? selectedCompany;

        /// <summary>Valitun yrityksen hakemukset uusin ensin.</summary>
        [ObservableProperty]
        private ListCollectionView? selectedCompanyApplications;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(OpenApplicationCommand))]
        private JobApplication? selectedCompanyApplication;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RenameIndustryCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteIndustryCommand))]
        private Industry? selectedIndustry;

        /// <summary>Valitun toimialan uusi nimi; esitäytetään valitun nimellä.</summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RenameIndustryCommand))]
        private string renameIndustryName = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddIndustryCommand))]
        private string newIndustryName = string.Empty;

        partial void OnSearchTextChanged(string value) => CompaniesView.Refresh();

        partial void OnSelectedCompanyChanged(Company? oldValue, Company? newValue)
        {
            if (oldValue is not null)
                oldValue.PropertyChanged -= OnSelectedCompanyPropertyChanged;
            if (newValue is not null)
                newValue.PropertyChanged += OnSelectedCompanyPropertyChanged;

            SelectedCompanyApplication = null;
            SelectedCompanyApplications = newValue is null ? null : CreateApplicationsView(newValue);
        }

        private void OnSelectedCompanyPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Company.Industry))
            {
                // Siirtää yrityksen toimialojen Companies-kokoelmissa heti, jotta lukumäärät pysyvät ajan tasalla.
                _db.ChangeTracker.DetectChanges();
                ClearIndustryCommand.NotifyCanExecuteChanged();
            }
        }

        partial void OnSelectedIndustryChanged(Industry? value) => RenameIndustryName = value?.Name ?? string.Empty;

        /// <summary>
        /// Kutsutaan, kun välilehti avataan: Hakemukset-välilehdellä on voitu vaihtaa hakemuksen yritystä
        /// tai lisätä yrityksiä, joten navigaatiot synkataan ja lista lajitellaan uudelleen.
        /// </summary>
        public void Activate()
        {
            _db.ChangeTracker.DetectChanges();
            RefreshViews();
        }

        // ---------- Haku ----------

        private bool FilterCompany(object item)
        {
            if (item is not Company company)
                return false;

            var term = SearchText.Trim();
            if (term.Length == 0)
                return true;

            return Matches(company.Name, term) || Matches(company.Industry?.Name, term);
        }

        private static bool Matches(string? text, string term)
            => text?.Contains(term, StringComparison.CurrentCultureIgnoreCase) == true;

        // ---------- Yritykset ----------

        [RelayCommand]
        private void NewCompany()
        {
            SearchText = string.Empty;

            var company = _db.Companies.CreateProxy();
            _db.Companies.Add(company);
            SelectedCompany = company;
        }

        [RelayCommand]
        private void Save()
        {
            if (TrySave())
                RefreshViews();
        }

        [RelayCommand(CanExecute = nameof(HasSelectedCompany))]
        private void DeleteCompany()
        {
            if (SelectedCompany is not { } company)
                return;

            _db.ChangeTracker.DetectChanges();
            var applications = company.JobApplications.ToList();
            var name = DisplayName(company.Name);

            var message = applications.Count switch
            {
                0 => $"Poistetaanko yritys \"{name}\"?",
                1 => $"Yrityksellä \"{name}\" on 1 hakemus. Hakemus säilyy, mutta se jää ilman yritystä.\n\n" +
                     "Poistetaanko yritys silti?",
                var n => $"Yrityksellä \"{name}\" on {n} hakemusta. Hakemukset säilyvät, mutta ne jäävät ilman yritystä.\n\n" +
                         "Poistetaanko yritys silti?"
            };
            if (!_dialogs.Confirm(message, "Poista yritys"))
                return;

            SelectedCompany = null;
            // Irrotetaan hakemukset itse, jotta Hakemukset-lista päivittyy heti (yritys-sarake tyhjenee).
            foreach (var application in applications)
                application.Company = null;
            _db.Companies.Remove(company);
            TrySave();
        }

        [RelayCommand(CanExecute = nameof(CanClearIndustry))]
        private void ClearIndustry()
        {
            if (SelectedCompany is not null)
                SelectedCompany.Industry = null;
        }

        [RelayCommand(CanExecute = nameof(HasSelectedCompanyApplication))]
        private void OpenApplication()
        {
            if (SelectedCompanyApplication is { } application)
                OpenApplicationRequested?.Invoke(application);
        }

        // ---------- Toimialat (tallentuvat heti) ----------

        [RelayCommand(CanExecute = nameof(CanAddIndustry))]
        private void AddIndustry()
        {
            var name = NewIndustryName.Trim();

            // Samannimistä ei luoda toista kertaa: valitaan olemassa oleva.
            var industry = FindIndustry(name);
            if (industry is null)
            {
                industry = _db.Industries.CreateProxy();
                industry.Name = name;
                _db.Industries.Add(industry);
                if (!TrySave())
                    return;
            }

            NewIndustryName = string.Empty;
            SelectedIndustry = industry;
        }

        [RelayCommand(CanExecute = nameof(CanRenameIndustry))]
        private void RenameIndustry()
        {
            if (SelectedIndustry is not { } industry)
                return;

            var name = RenameIndustryName.Trim();
            var existing = FindIndustry(name);
            if (existing is not null && existing != industry)
            {
                _dialogs.ShowError($"Toimiala \"{existing.Name}\" on jo olemassa.", "Nimeä toimiala");
                return;
            }

            industry.Name = name;
            if (TrySave())
                RefreshViews();
            RenameIndustryName = industry.Name;
        }

        [RelayCommand(CanExecute = nameof(HasSelectedIndustry))]
        private void DeleteIndustry()
        {
            if (SelectedIndustry is not { } industry)
                return;

            _db.ChangeTracker.DetectChanges();
            var companies = industry.Companies.ToList();
            var name = DisplayName(industry.Name);

            var message = companies.Count switch
            {
                0 => $"Poistetaanko toimiala \"{name}\"?",
                1 => $"Toimialalla \"{name}\" on 1 yritys. Yritys säilyy, mutta siltä poistuu toimiala.\n\n" +
                     "Poistetaanko toimiala silti?",
                var n => $"Toimialalla \"{name}\" on {n} yritystä. Yritykset säilyvät, mutta niiltä poistuu toimiala.\n\n" +
                         "Poistetaanko toimiala silti?"
            };
            if (!_dialogs.Confirm(message, "Poista toimiala"))
                return;

            SelectedIndustry = null;
            foreach (var company in companies)
                company.Industry = null;
            _db.Industries.Remove(industry);
            TrySave();
        }

        // ---------- Apurit ----------

        private bool HasSelectedCompany() => SelectedCompany is not null;

        private bool CanClearIndustry() => SelectedCompany?.Industry is not null;

        private bool HasSelectedCompanyApplication() => SelectedCompanyApplication is not null;

        private bool HasSelectedIndustry() => SelectedIndustry is not null;

        private bool CanAddIndustry() => !string.IsNullOrWhiteSpace(NewIndustryName);

        private bool CanRenameIndustry()
            => SelectedIndustry is not null
               && !string.IsNullOrWhiteSpace(RenameIndustryName)
               && RenameIndustryName.Trim() != SelectedIndustry.Name;

        private Industry? FindIndustry(string name)
            => _db.Industries.Local.FirstOrDefault(
                i => string.Equals(i.Name.Trim(), name, StringComparison.CurrentCultureIgnoreCase));

        private static ListCollectionView CreateApplicationsView(Company company)
        {
            // Kokoelma on entiteetissä alustettu ObservableCollectioniksi, joten näkymä seuraa muutoksia.
            var source = company.JobApplications as IList ?? company.JobApplications.ToList();
            var view = new ListCollectionView(source);
            view.SortDescriptions.Add(
                new SortDescription(nameof(JobApplication.ApplicationDate), ListSortDirection.Descending));
            return view;
        }

        /// <summary>Lajittelu ja suodatus eivät päivity elävästi muokatessa, joten päivitetään valinnat säilyttäen.</summary>
        private void RefreshViews()
        {
            var company = SelectedCompany;
            var industry = SelectedIndustry;
            var application = SelectedCompanyApplication;

            CompaniesView.Refresh();
            IndustriesView.Refresh();
            SelectedCompanyApplications?.Refresh();

            if (company is not null && CompaniesView.Contains(company))
                SelectedCompany = company;
            if (industry is not null && IndustriesView.Contains(industry))
                SelectedIndustry = industry;
            if (application is not null && SelectedCompanyApplications?.Contains(application) == true)
                SelectedCompanyApplication = application;
        }

        private bool TrySave() => _db.TrySave(_dialogs);

        private static string DisplayName(string name) => string.IsNullOrWhiteSpace(name) ? "(nimetön)" : name;
    }
}

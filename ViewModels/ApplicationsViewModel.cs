using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JobApplicationManager.Context;
using JobApplicationManager.Helpers;
using JobApplicationManager.Model;
using JobApplicationManager.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Data;

namespace JobApplicationManager.ViewModels
{
    /// <summary>
    /// Hakemukset-välilehti: lista + haku/suodatus vasemmalla, valitun hakemuksen lomake oikealla.
    /// Muutokset kertyvät AppDbContextiin ja tallentuvat Tallenna-komennolla (Ctrl+S).
    /// </summary>
    public partial class ApplicationsViewModel : ObservableObject
    {
        private readonly AppDbContext _db;
        private readonly IDialogService _dialogs;

        public ApplicationsViewModel(AppDbContext db, IDialogService dialogs)
        {
            _db = db;
            _dialogs = dialogs;

            // Yritykset ensin, jotta hakemusten Company-viittaukset täyttyvät suoraan muistissa olevista olioista.
            _db.Companies.Load();
            _db.JobApplications.Load();

            Applications = _db.JobApplications.Local.ToObservableCollection();
            ApplicationsView = new ListCollectionView(Applications) { Filter = FilterApplication };
            ApplicationsView.SortDescriptions.Add(
                new SortDescription(nameof(JobApplication.ApplicationDate), ListSortDirection.Descending));

            CompaniesView = new ListCollectionView(_db.Companies.Local.ToObservableCollection());
            CompaniesView.SortDescriptions.Add(new SortDescription(nameof(Company.Name), ListSortDirection.Ascending));

            StatusFilterOptions =
            [
                new StatusFilterOption("Kaikki tilat", null),
                .. Statuses.Select(s => new StatusFilterOption(s.GetDescription(), s))
            ];
            SelectedStatusFilter = StatusFilterOptions[0];
        }

        /// <summary>Synkassa DbContextin kanssa: lisäys/poisto kontekstiin näkyy tässä ja päinvastoin.</summary>
        public ObservableCollection<JobApplication> Applications { get; }

        /// <summary>Listan näkymä: lajittelu + haku- ja tilasuodatus.</summary>
        public ListCollectionView ApplicationsView { get; }

        /// <summary>Yritysvalinnan lista aakkosjärjestyksessä.</summary>
        public ListCollectionView CompaniesView { get; }

        public IReadOnlyList<ApplicationStatus> Statuses { get; } = Enum.GetValues<ApplicationStatus>();

        public IReadOnlyList<StatusFilterOption> StatusFilterOptions { get; }

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private StatusFilterOption? selectedStatusFilter;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
        [NotifyCanExecuteChangedFor(nameof(StartAddCompanyCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenPostingUrlCommand))]
        private JobApplication? selectedApplication;

        [ObservableProperty]
        private bool isAddingCompany;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmAddCompanyCommand))]
        private string newCompanyName = string.Empty;

        partial void OnSearchTextChanged(string value) => ApplicationsView.Refresh();

        partial void OnSelectedStatusFilterChanged(StatusFilterOption? value) => ApplicationsView.Refresh();

        partial void OnSelectedApplicationChanged(JobApplication? oldValue, JobApplication? newValue)
        {
            if (oldValue is not null)
                oldValue.PropertyChanged -= OnSelectedApplicationPropertyChanged;
            if (newValue is not null)
                newValue.PropertyChanged += OnSelectedApplicationPropertyChanged;

            IsAddingCompany = false;
        }

        private void OnSelectedApplicationPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(JobApplication.JobPostingUrl))
                OpenPostingUrlCommand.NotifyCanExecuteChanged();
        }

        // ---------- Haku ja suodatus ----------

        private bool FilterApplication(object item)
        {
            if (item is not JobApplication application)
                return false;

            if (SelectedStatusFilter?.Status is { } status && application.Status != status)
                return false;

            var term = SearchText.Trim();
            if (term.Length == 0)
                return true;

            return Matches(application.Position, term)
                || Matches(application.Company?.Name, term)
                || Matches(application.Location, term)
                || Matches(application.ContactName, term)
                || Matches(application.Notes, term);
        }

        private static bool Matches(string? text, string term)
            => text?.Contains(term, StringComparison.CurrentCultureIgnoreCase) == true;

        // ---------- Komennot ----------

        [RelayCommand]
        private void NewApplication()
        {
            // Tyhjennetään suodattimet, jotta uusi hakemus näkyy listassa varmasti.
            SearchText = string.Empty;
            SelectedStatusFilter = StatusFilterOptions[0];

            // CreateProxy (eikä new), jotta lazy loading toimii uudellekin oliolle.
            var application = _db.JobApplications.CreateProxy();
            _db.JobApplications.Add(application);
            SelectedApplication = application;
        }

        [RelayCommand]
        private void Save()
        {
            if (TrySave())
                RefreshViews();
        }

        /// <summary>
        /// Kutsutaan, kun välilehti avataan: Yritykset-välilehdellä on voitu nimetä tai poistaa yrityksiä,
        /// joten lista ja yritysvalinta lajitellaan uudelleen.
        /// </summary>
        public void Activate() => RefreshViews();

        /// <summary>Valitsee hakemuksen (esim. Yritykset-välilehdeltä); tyhjentää suodattimet, jos se on piilossa.</summary>
        public void ShowApplication(JobApplication application)
        {
            if (!FilterApplication(application))
            {
                SearchText = string.Empty;
                SelectedStatusFilter = StatusFilterOptions[0];
            }
            SelectedApplication = application;
        }

        [RelayCommand(CanExecute = nameof(HasSelectedApplication))]
        private void Delete()
        {
            if (SelectedApplication is not { } application)
                return;

            var confirmed = _dialogs.Confirm(
                $"Poistetaanko hakemus \"{Describe(application)}\"?\n\n" +
                "Myös hakemuksen tapahtumaloki poistetaan, eikä poistoa voi perua.",
                "Poista hakemus");
            if (!confirmed)
                return;

            SelectedApplication = null;
            _db.JobApplications.Remove(application);
            TrySave();
        }

        [RelayCommand(CanExecute = nameof(CanOpenPostingUrl))]
        private void OpenPostingUrl()
        {
            if (!TryGetPostingUri(out var uri))
                return;

            try
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _dialogs.ShowError($"Linkin avaaminen epäonnistui:\n\n{ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(HasSelectedApplication))]
        private void StartAddCompany()
        {
            NewCompanyName = string.Empty;
            IsAddingCompany = true;
        }

        [RelayCommand(CanExecute = nameof(CanConfirmAddCompany))]
        private void ConfirmAddCompany()
        {
            if (SelectedApplication is null)
                return;

            var name = NewCompanyName.Trim();

            // Jos samanniminen yritys on jo olemassa, käytetään sitä eikä luoda kaksoiskappaletta.
            var company = _db.Companies.Local.FirstOrDefault(
                c => string.Equals(c.Name.Trim(), name, StringComparison.CurrentCultureIgnoreCase));
            if (company is null)
            {
                company = _db.Companies.CreateProxy();
                company.Name = name;
                _db.Companies.Add(company);
            }

            SelectedApplication.Company = company;
            IsAddingCompany = false;
        }

        [RelayCommand]
        private void CancelAddCompany() => IsAddingCompany = false;

        // ---------- Apurit ----------

        private bool HasSelectedApplication() => SelectedApplication is not null;

        private bool CanConfirmAddCompany() => !string.IsNullOrWhiteSpace(NewCompanyName);

        private bool CanOpenPostingUrl() => TryGetPostingUri(out _);

        /// <summary>
        /// Hyväksyy vain http/https-osoitteet, ettei linkkikentän kautta voi käynnistää paikallisia ohjelmia.
        /// Osoite ilman skeemaa (esim. "www.duunitori.fi/...") tulkitaan https-osoitteeksi.
        /// </summary>
        private bool TryGetPostingUri([NotNullWhen(true)] out Uri? uri)
        {
            uri = null;
            var text = SelectedApplication?.JobPostingUrl?.Trim();
            if (string.IsNullOrEmpty(text))
                return false;

            if (!text.Contains("://", StringComparison.Ordinal))
                text = "https://" + text;

            return Uri.TryCreate(text, UriKind.Absolute, out uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>Lajittelu ja suodatus eivät päivity elävästi muokatessa, joten päivitetään valinta säilyttäen.</summary>
        private void RefreshViews()
        {
            var selected = SelectedApplication;
            ApplicationsView.Refresh();
            CompaniesView.Refresh();
            if (selected is not null && ApplicationsView.Contains(selected))
                SelectedApplication = selected;
        }

        private bool TrySave()
        {
            try
            {
                _db.SaveChanges();
                return true;
            }
            catch (DbUpdateException ex)
            {
                _dialogs.ShowError($"Tallennus epäonnistui:\n\n{ex.InnerException?.Message ?? ex.Message}");
                return false;
            }
        }

        private static string Describe(JobApplication application)
        {
            var parts = new[] { application.Position, application.Company?.Name }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            var text = string.Join(" – ", parts);
            return text.Length > 0 ? text : "(nimetön)";
        }
    }
}

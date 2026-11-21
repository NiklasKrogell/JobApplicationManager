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
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Windows.Data;

namespace JobApplicationManager.ViewModels
{
    /// <summary>
    /// Hakemukset-välilehti: lista + haku/suodatus vasemmalla, valitun hakemuksen lomake oikealla.
    /// Muutokset kertyvät AppDbContextiin ja tallentuvat Tallenna-komennolla (Ctrl+S).
    /// </summary>
    public partial class ApplicationsViewModel : ObservableObject
    {
        // Ääkköset tallennetaan kantaan luettavina (oletuksena System.Text.Json kirjoittaisi ne ä-muodossa).
        private static readonly JsonSerializerOptions KeywordsJsonOptions =
            new() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };

        private readonly AppDbContext _db;
        private readonly IDialogService _dialogs;
        private readonly IKeywordExtractionService _keywordExtraction;

        public ApplicationsViewModel(AppDbContext db, IDialogService dialogs, IKeywordExtractionService keywordExtraction)
        {
            _db = db;
            _dialogs = dialogs;
            _keywordExtraction = keywordExtraction;

            // Yritykset ensin, jotta hakemusten Company-viittaukset täyttyvät suoraan muistissa olevista olioista.
            _db.Companies.Load();
            // Tapahtumat samalla kyselyllä: valitun hakemuksen loki näkyy heti ilman erillistä latausta.
            _db.JobApplications.Include(a => a.Activities).Load();

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
        [NotifyCanExecuteChangedFor(nameof(ExtractKeywordsCommand))]
        private JobApplication? selectedApplication;

        [ObservableProperty]
        private bool isAddingCompany;

        // ---------- Työpaikkailmoituksen avainsanat ----------

        /// <summary>Valitun hakemuksen avainsanat (KeywordsJson luettuna), tai null jos niitä ei ole poimittu.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(KeywordsLanguageText))]
        private JobKeywords? selectedKeywords;

        /// <summary>Ilmoituksen kieli suomeksi, esim. "englanti (en)".</summary>
        public string KeywordsLanguageText => SelectedKeywords?.Language switch
        {
            null or "" => "–",
            "fi" => "suomi (fi)",
            "en" => "englanti (en)",
            "sv" => "ruotsi (sv)",
            "de" => "saksa (de)",
            "et" => "viro (et)",
            var code => code
        };

        // ---------- Tapahtumaloki ----------

        /// <summary>Itse lisättävät tyypit; tilamuutokset kirjautuvat automaattisesti tallennettaessa.</summary>
        public IReadOnlyList<ActivityType> ActivityTypes { get; } =
            Enum.GetValues<ActivityType>().Where(t => t != ActivityType.StatusChange).ToList();

        /// <summary>Valitun hakemuksen tapahtumat uusin ensin.</summary>
        [ObservableProperty]
        private ListCollectionView? activitiesView;

        [ObservableProperty]
        private ActivityType newActivityType = ActivityType.Note;

        [ObservableProperty]
        private DateTime? newActivityDate = DateTime.Today;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddActivityCommand))]
        private string newActivityDescription = string.Empty;

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
            SelectedKeywords = ParseKeywords(newValue?.KeywordsJson);

            ActivitiesView = newValue is null ? null : CreateActivitiesView(newValue);
            NewActivityDate = DateTime.Today;
            NewActivityDescription = string.Empty;
            AddActivityCommand.NotifyCanExecuteChanged();
        }

        private static ListCollectionView CreateActivitiesView(JobApplication application)
        {
            // Kokoelma on entiteetissä ObservableCollection, joten myös automaattiset tilamuutosrivit
            // ilmestyvät listaan heti tallennuksen jälkeen.
            var source = application.Activities as System.Collections.IList ?? application.Activities.ToList();
            var view = new ListCollectionView(source);
            view.SortDescriptions.Add(new SortDescription(nameof(Activity.Date), ListSortDirection.Descending));
            view.SortDescriptions.Add(new SortDescription(nameof(Activity.Id), ListSortDirection.Descending));
            return view;
        }

        private void OnSelectedApplicationPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(JobApplication.JobPostingUrl):
                    OpenPostingUrlCommand.NotifyCanExecuteChanged();
                    break;
                case nameof(JobApplication.JobPostingText):
                    ExtractKeywordsCommand.NotifyCanExecuteChanged();
                    break;
                case nameof(JobApplication.KeywordsJson):
                    SelectedKeywords = ParseKeywords(SelectedApplication?.KeywordsJson);
                    break;
            }
        }

        private static JobKeywords? ParseKeywords(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<JobKeywords>(json);
            }
            catch (JsonException)
            {
                return null;
            }
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

        /// <summary>Vie listalla näkyvät hakemukset (haku ja tilasuodatin huomioiden) CSV-tiedostoon.</summary>
        [RelayCommand]
        private void ExportCsv()
        {
            var rows = ApplicationsView.Cast<JobApplication>().ToList();
            if (rows.Count == 0)
            {
                _dialogs.ShowInfo("Listalla ei ole hakemuksia vietäväksi.", "Vie CSV");
                return;
            }

            var path = _dialogs.AskSaveFilePath($"hakemukset-{DateTime.Today:yyyy-MM-dd}.csv",
                "CSV-tiedosto (*.csv)|*.csv", "Vie hakemukset CSV-tiedostoon");
            if (path is null)
                return;

            try
            {
                CsvExporter.Export(rows, path);
                _dialogs.ShowInfo($"Vietiin {rows.Count} hakemusta tiedostoon:\n{path}", "Vie CSV");
            }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
            {
                _dialogs.ShowError($"Tiedoston kirjoitus epäonnistui:\n\n{ex.Message}");
            }
        }

        /// <summary>
        /// Kopioi koko tietokannan (myös yritykset, toimialat ja tapahtumat) valittuun tiedostoon.
        /// SQLiten VACUUM INTO tekee eheän kopion, vaikka kanta on auki.
        /// </summary>
        [RelayCommand]
        private void BackupDatabase()
        {
            if (_db.ChangeTracker.HasChanges() &&
                !_dialogs.Confirm("Varmuuskopioon tulee vain tallennettu data, ei tallentamattomia muutoksia.\n\nJatketaanko?",
                    "Varmuuskopioi"))
                return;

            var path = _dialogs.AskSaveFilePath($"tyonhaku-varmuuskopio-{DateTime.Today:yyyy-MM-dd}.db",
                "SQLite-tietokanta (*.db)|*.db", "Varmuuskopioi tietokanta");
            if (path is null)
                return;

            try
            {
                // VACUUM INTO ei kirjoita olemassa olevan päälle; tallennusdialogi on jo kysynyt korvaamisesta.
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
                _db.Database.ExecuteSql($"VACUUM INTO {path}");
                _dialogs.ShowInfo($"Varmuuskopio tallennettu:\n{path}\n\n" +
                    "Palautus: sulje sovellus ja kopioi tiedosto nimellä job_applications.db kansioon\n" +
                    System.IO.Path.GetDirectoryName(AppDbContext.DatabasePath), "Varmuuskopioi");
            }
            catch (Exception ex)
            {
                _dialogs.ShowError($"Varmuuskopiointi epäonnistui:\n\n{ex.Message}");
            }
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

        /// <summary>
        /// Poimii ilmoitustekstistä avainsanat Claudella. Tulos jää hakemukselle tallentamattomaksi muutokseksi
        /// kuten muutkin lomakkeen muokkaukset (Tallenna / Ctrl+S). Komento on pois käytöstä poiminnan ajan.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExtractKeywords))]
        private async Task ExtractKeywordsAsync()
        {
            if (SelectedApplication is not { } application)
                return;

            if (!string.IsNullOrEmpty(application.KeywordsJson) &&
                !_dialogs.Confirm("Hakemukselle on jo poimittu avainsanat.\n\nKorvataanko ne uusilla?", "Poimi avainsanat"))
                return;

            try
            {
                var keywords = await _keywordExtraction.ExtractAsync(application.JobPostingText ?? string.Empty);
                // Kirjoitetaan talteen otetulle hakemukselle, vaikka valinta olisi ehtinyt vaihtua odotuksen aikana.
                application.KeywordsJson = JsonSerializer.Serialize(keywords, KeywordsJsonOptions);
            }
            catch (KeywordExtractionException ex)
            {
                _dialogs.ShowError(ex.Message, "Avainsanojen poiminta");
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

        /// <summary>Lisää tapahtuman ja tallentaa heti (samalla tallentuvat lomakkeen muutokset).</summary>
        [RelayCommand(CanExecute = nameof(CanAddActivity))]
        private void AddActivity()
        {
            if (SelectedApplication is not { } application)
                return;

            // Uusi hakemus tallennetaan ensin, jotta "Hakemus luotu" -rivi kirjautuu ennen tätä tapahtumaa.
            if (_db.Entry(application).State == EntityState.Added && !TrySave())
                return;

            // Päivä valitaan kalenterista; kellonaika pitää saman päivän tapahtumat lisäysjärjestyksessä.
            var day = (NewActivityDate ?? DateTime.Today).Date;
            var activity = new Activity
            {
                JobApplication = application,
                Date = day + DateTime.Now.TimeOfDay,
                Type = NewActivityType,
                Description = NewActivityDescription.Trim()
            };
            _db.Activities.Add(activity);

            if (TrySave())
            {
                NewActivityDescription = string.Empty;
                RefreshViews();
            }
        }

        [RelayCommand]
        private void DeleteActivity(Activity? activity)
        {
            if (activity is null)
                return;

            var confirmed = _dialogs.Confirm(
                $"Poistetaanko tapahtuma \"{activity.Description}\" ({activity.Date:d})?",
                "Poista tapahtuma");
            if (!confirmed)
                return;

            _db.Activities.Remove(activity);
            TrySave();
        }

        // ---------- Apurit ----------

        private bool HasSelectedApplication() => SelectedApplication is not null;

        private bool CanConfirmAddCompany() => !string.IsNullOrWhiteSpace(NewCompanyName);

        private bool CanAddActivity()
            => SelectedApplication is not null && !string.IsNullOrWhiteSpace(NewActivityDescription);

        private bool CanOpenPostingUrl() => TryGetPostingUri(out _);

        private bool CanExtractKeywords() => !string.IsNullOrWhiteSpace(SelectedApplication?.JobPostingText);

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
            ActivitiesView?.Refresh();
            if (selected is not null && ApplicationsView.Contains(selected))
                SelectedApplication = selected;
        }

        private bool TrySave() => _db.TrySave(_dialogs);

        private static string Describe(JobApplication application)
        {
            var parts = new[] { application.Position, application.Company?.Name }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            var text = string.Join(" – ", parts);
            return text.Length > 0 ? text : "(nimetön)";
        }
    }
}

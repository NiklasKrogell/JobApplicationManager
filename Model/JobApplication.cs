using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace JobApplicationManager.Model
{
    /// <summary>
    /// Hakemuksen tila. Tallennetaan kantaan nimenä, joten järjestystä voi muuttaa ja uusia tiloja lisätä
    /// ilman migraatiota. Järjestys on myös näytettävä järjestys (valintalista, suodatin, tilastot).
    /// </summary>
    public enum ApplicationStatus
    {
        /// <summary>Ilmoitus on kirjattu, mutta hakemusta ei ole vielä lähetetty.</summary>
        [Description("Uusi")]
        New,
        [Description("Haettu")]
        Applied,
        [Description("Haastattelu")]
        Interviewing,
        [Description("Tarjous")]
        Offered,
        [Description("Hyväksytty")]
        Accepted,
        [Description("Hylätty")]
        Rejected,
        [Description("Vetäydyin")]
        Withdrawn
    }

    /// <summary>
    /// ObservableObject: käyttöliittymä päivittyy, kun ominaisuuksia muutetaan koodista
    /// (esim. kun hakemukselle asetetaan juuri luotu yritys).
    /// </summary>
    public class JobApplication : ObservableObject
    {
        private Company? _company;
        private string? _position;
        private DateTime _applicationDate = DateTime.Today;
        private ApplicationStatus _status = ApplicationStatus.New;
        private string? _location;
        private string? _jobPostingUrl;
        private string? _notes;
        private string? _contactName;
        private string? _contactEmail;
        private string? _contactPhone;
        private string? _jobPostingText;
        private string? _keywordsJson;

        public int Id { get; set; }
        public int? CompanyId { get; set; }

        public virtual Company? Company
        {
            get => _company;
            set => SetProperty(ref _company, value);
        }

        public string? Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        public DateTime ApplicationDate
        {
            get => _applicationDate;
            set => SetProperty(ref _applicationDate, value);
        }

        public ApplicationStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string? Location
        {
            get => _location;
            set => SetProperty(ref _location, value);
        }

        public string? JobPostingUrl
        {
            get => _jobPostingUrl;
            set => SetProperty(ref _jobPostingUrl, value);
        }

        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public string? ContactName
        {
            get => _contactName;
            set => SetProperty(ref _contactName, value);
        }

        public string? ContactEmail
        {
            get => _contactEmail;
            set => SetProperty(ref _contactEmail, value);
        }

        public string? ContactPhone
        {
            get => _contactPhone;
            set => SetProperty(ref _contactPhone, value);
        }

        /// <summary>Työpaikkailmoituksen teksti, josta avainsanat poimitaan.</summary>
        public string? JobPostingText
        {
            get => _jobPostingText;
            set => SetProperty(ref _jobPostingText, value);
        }

        /// <summary>Clauden poimimat avainsanat JSON-muodossa (ks. JobKeywords).</summary>
        public string? KeywordsJson
        {
            get => _keywordsJson;
            set => SetProperty(ref _keywordsJson, value);
        }

        public virtual ICollection<Activity> Activities { get; set; } = new ObservableCollection<Activity>();
    }
}

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace JobApplicationManager.Model
{
    public enum ApplicationStatus
    {
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

    public class JobApplication
    {
        public int Id { get; set; }
        public int? CompanyId { get; set; }
        public virtual Company? Company { get; set; }
        public string? Position { get; set; }
        public DateTime ApplicationDate { get; set; } = DateTime.Today;
        public ApplicationStatus Status { get; set; }
        public string? Location { get; set; }
        public string? JobPostingUrl { get; set; }
        public string? Notes { get; set; }
        public string? ContactName { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public virtual ICollection<Activity> Activities { get; set; } = new ObservableCollection<Activity>();
    }
}

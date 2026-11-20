using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace JobApplicationManager.Model
{
    public class Company : ObservableObject
    {
        private string _name = string.Empty;
        private Industry? _industry;

        public int Id { get; set; }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public int? IndustryId { get; set; }

        public virtual Industry? Industry
        {
            get => _industry;
            set => SetProperty(ref _industry, value);
        }

        public virtual ICollection<JobApplication> JobApplications { get; set; } = new ObservableCollection<JobApplication>();

        // Näkyy mm. ruudunlukijoille ja debuggerissa proxy-luokan nimen sijaan.
        public override string ToString() => Name;
    }
}

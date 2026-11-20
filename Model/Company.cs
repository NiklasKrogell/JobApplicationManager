using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace JobApplicationManager.Model
{
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? IndustryId { get; set; }
        public virtual Industry? Industry { get; set; }
        public virtual ICollection<JobApplication> JobApplications { get; set; } = new ObservableCollection<JobApplication>();
    }
}

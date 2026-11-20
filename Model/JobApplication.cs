using System;
using System.Collections.Generic;
using System.Text;

namespace JobApplicationManager.Model
{
    public enum ApplicationStatus
    {
        Applied,
        Interviewing,
        Offered,
        Rejected
    }
    public class JobApplication
    {
        public int Id { get; set; }
        public int? CompanyId { get; set; }
        public virtual Company? Company { get; set; }
        public string? Position { get; set; }
        public DateTime ApplicationDate { get; set; }
        public ApplicationStatus Status { get; set; }
        public string? Notes { get; set; }
        public string? ContactName { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
    }
}

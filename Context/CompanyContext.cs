using JobApplicationManager.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace JobApplicationManager.Context
{
    public class CompanyContext : DbContext
    {
        public DbSet<Company> Companies { get; set; }
        public DbSet<Industry> Industries { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=job_applications.db");
            optionsBuilder.UseLazyLoadingProxies();
        }
    }
}

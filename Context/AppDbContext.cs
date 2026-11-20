using JobApplicationManager.Model;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace JobApplicationManager.Context
{
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Tietokanta sijaitsee käyttäjän LocalAppData-kansiossa, jotta data säilyy
        /// vaikka bin/obj-kansiot siivottaisiin tai sovellus buildattaisiin uudelleen.
        /// </summary>
        public static string DatabasePath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "JobApplicationManager",
            "job_applications.db");

        public DbSet<Company> Companies { get; set; }
        public DbSet<Industry> Industries { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
            optionsBuilder.UseSqlite($"Data Source={DatabasePath}");
            optionsBuilder.UseLazyLoadingProxies();
        }
    }
}
